using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Core.Enums;
using Premag.Core.Exceptions;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

/// <summary>
/// Fecha o dia da equipe (ou da planta) para apontamento. RN-15 também fecha pelo calendário (DiasFechamento).
/// </summary>
public class FechamentoService : IFechamentoService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;

    public FechamentoService(ApplicationDbContext db, IRelogio relogio)
    {
        _db = db;
        _relogio = relogio;
    }

    public async Task GarantirAbertoAsync(DateOnly data, Guid? equipeId, CancellationToken cancellationToken = default)
    {
        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        if (CalculoFechamento.FechadoPorCalendario(data, _relogio.HojeSaoPaulo, config.DiasFechamento))
            throw new RegraNegocioException("RN-15", "Este dia já está fechado para apontamento.", 422);

        var registros = await _db.FechamentosDia.AsNoTracking()
            .Where(f => f.Data == data)
            .Select(f => new RegistroFechamento(f.Data, f.EquipeId, f.ReabertoEm))
            .ToListAsync(cancellationToken);

        if (CalculoFechamento.FechadoPorRegistro(data, equipeId, registros))
            throw new RegraNegocioException("RN-15", "Este dia já foi fechado. Peça à gerência para reabrir, se precisar apontar.", 422);
    }

    public async Task<FechamentoDiaDto> ObterAsync(
        DateOnly? data,
        Guid? equipeId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var dia = data ?? _relogio.HojeSaoPaulo;
        var equipe = ResolverEquipeConsulta(equipeId, quem);
        return await MontarAsync(dia, equipe, cancellationToken);
    }

    public async Task<FechamentoDiaDto> FecharAsync(
        FecharDiaDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var dia = dto.Data ?? _relogio.HojeSaoPaulo;
        var equipeId = ResolverEquipeMutacao(dto.EquipeId, quem);

        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        if (CalculoFechamento.FechadoPorCalendario(dia, _relogio.HojeSaoPaulo, config.DiasFechamento))
            throw new RegraNegocioException("RN-15", "Este dia já está fechado pelo prazo de calendário.", 422);

        var resumo = await MontarAsync(dia, equipeId, cancellationToken);
        if (resumo.ApontamentosAbertos > 0)
            throw new RegraNegocioException("DIA_COM_SERVICO_ABERTO", "Encerre os serviços abertos antes de fechar o dia.", 422);

        if (resumo.Fechado && !resumo.FechadoPorCalendario)
            return resumo;

        var row = await _db.FechamentosDia
            .FirstOrDefaultAsync(f => f.Data == dia && f.EquipeId == equipeId, cancellationToken);

        if (row is null)
        {
            row = new FechamentoDia
            {
                TenantId = _db.TenantId,
                Data = dia,
                EquipeId = equipeId,
                FechadoEm = _relogio.UtcAgora,
                FechadoPorId = quem.Id
            };
            _db.FechamentosDia.Add(row);
        }
        else
        {
            row.FechadoEm = _relogio.UtcAgora;
            row.FechadoPorId = quem.Id;
            row.ReabertoEm = null;
            row.ReabertoPorId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MontarAsync(dia, equipeId, cancellationToken);
    }

    public async Task<FechamentoDiaDto> ReabrirAsync(
        ReabrirDiaDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("RN-15", "Só a gerência reabre um dia fechado.", 403);

        var motivo = (dto.MotivoReabertura ?? "").Trim();
        if (motivo.Length < 3)
            throw new RegraNegocioException("RN-15", "Informe o motivo da reabertura.", 422);

        var dia = dto.Data ?? _relogio.HojeSaoPaulo;
        var equipeId = dto.EquipeId;

        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        if (CalculoFechamento.FechadoPorCalendario(dia, _relogio.HojeSaoPaulo, config.DiasFechamento))
            throw new RegraNegocioException("RN-15", "O prazo de calendário já fechou este dia; não dá para reabrir.", 422);

        var row = await _db.FechamentosDia
            .FirstOrDefaultAsync(f => f.Data == dia && f.EquipeId == equipeId && f.ReabertoEm == null, cancellationToken)
            ?? throw new RegraNegocioException("DIA_NAO_FECHADO", "Este dia não está fechado por registro.", 422);

        row.ReabertoEm = _relogio.UtcAgora;
        row.ReabertoPorId = quem.Id;
        row.MotivoReabertura = motivo;
        await _db.SaveChangesAsync(cancellationToken);
        return await MontarAsync(dia, equipeId, cancellationToken);
    }

    private Guid? ResolverEquipeConsulta(Guid? pedida, UsuarioLogado quem)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            return quem.EquipeId
                ?? throw new RegraNegocioException("EQUIPE_OBRIGATORIA", "Encarregado precisa estar ligado a uma equipe.", 422);
        return pedida;
    }

    private Guid? ResolverEquipeMutacao(Guid? pedida, UsuarioLogado quem)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            if (pedida is Guid id && quem.EquipeId is Guid propria && id != propria)
                throw new RegraNegocioException("RN-10", "Encarregado só fecha a própria equipe.", 403);
            return quem.EquipeId
                ?? throw new RegraNegocioException("EQUIPE_OBRIGATORIA", "Encarregado precisa estar ligado a uma equipe.", 422);
        }

        return pedida;
    }

    private async Task<FechamentoDiaDto> MontarAsync(DateOnly dia, Guid? equipeId, CancellationToken cancellationToken)
    {
        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        var porCalendario = CalculoFechamento.FechadoPorCalendario(dia, _relogio.HojeSaoPaulo, config.DiasFechamento);

        var registros = await _db.FechamentosDia.AsNoTracking()
            .Include(f => f.FechadoPor)
            .Where(f => f.Data == dia)
            .ToListAsync(cancellationToken);

        var vigente = registros
            .Where(f => f.ReabertoEm is null && (f.EquipeId is null || f.EquipeId == equipeId))
            .OrderByDescending(f => f.EquipeId is null) // planta primeiro se ambos
            .ThenByDescending(f => f.FechadoEm)
            .FirstOrDefault();

        var reaberto = registros
            .Where(f => f.EquipeId == equipeId && f.ReabertoEm is not null)
            .OrderByDescending(f => f.ReabertoEm)
            .FirstOrDefault();

        string escopo;
        if (equipeId is Guid eq)
        {
            var nome = await _db.Equipes.AsNoTracking()
                .Where(e => e.Id == eq)
                .Select(e => e.Nome)
                .FirstOrDefaultAsync(cancellationToken);
            escopo = nome ?? "Equipe";
        }
        else
        {
            escopo = "Planta";
        }

        var colabQuery = _db.Colaboradores.AsNoTracking().Where(c => c.Ativo);
        if (equipeId is Guid filtroEq)
            colabQuery = colabQuery.Where(c => c.EquipeId == filtroEq);
        var colabs = await colabQuery.Select(c => new { c.Id, c.EquipeId }).ToListAsync(cancellationToken);
        var ids = colabs.Select(c => c.Id).ToList();

        var apontamentos = ids.Count == 0
            ? []
            : await _db.Apontamentos.AsNoTracking()
                .Where(a => a.Data == dia && ids.Contains(a.ColaboradorId))
                .ToListAsync(cancellationToken);

        var jornadas = ids.Count == 0
            ? []
            : await _db.JornadasDia.AsNoTracking()
                .Where(j => j.Data == dia && ids.Contains(j.ColaboradorId))
                .ToListAsync(cancellationToken);

        var abertos = apontamentos.Count(a => a.HoraFim is null);
        var comServico = apontamentos.Select(a => a.ColaboradorId).ToHashSet();
        var presentesSem = colabs.Count(c =>
        {
            var j = jornadas.FirstOrDefault(x => x.ColaboradorId == c.Id);
            var sit = j?.Situacao ?? SituacaoJornada.Presente;
            return sit == SituacaoJornada.Presente && !comServico.Contains(c.Id);
        });

        var (inicio, fim) = JanelaDia.Utc(dia, _relogio.AgoraSaoPaulo.Offset);
        var fotosQ = _db.Fotos.AsNoTracking().Where(f => f.CapturadaEm >= inicio && f.CapturadaEm < fim);
        if (equipeId is Guid eqFoto)
        {
            fotosQ = fotosQ.Where(f =>
                (f.Colaborador != null && f.Colaborador.EquipeId == eqFoto) || f.Frente.EquipeId == eqFoto);
        }
        var fotos = await fotosQ.CountAsync(cancellationToken);

        var horas = apontamentos.Where(a => a.HoraFim is not null).Sum(a => (a.MinutosEfetivos ?? 0) / 60m);

        return new FechamentoDiaDto
        {
            Data = dia,
            EquipeId = equipeId,
            Escopo = escopo,
            Fechado = porCalendario || vigente is not null,
            FechadoPorCalendario = porCalendario,
            FechadoEm = vigente?.FechadoEm,
            FechadoPorNome = vigente?.FechadoPor?.NomeExibicao,
            ReabertoEm = vigente is null ? reaberto?.ReabertoEm : null,
            MotivoReabertura = vigente is null ? reaberto?.MotivoReabertura : null,
            DiasFechamento = config.DiasFechamento,
            ApontamentosAbertos = abertos,
            PresentesSemServico = presentesSem,
            Fotos = fotos,
            HorasApontadas = decimal.Round(horas, 1)
        };
    }
}
