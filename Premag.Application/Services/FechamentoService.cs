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
    private readonly IPushService _push;

    public FechamentoService(ApplicationDbContext db, IRelogio relogio, IPushService push)
    {
        _db = db;
        _relogio = relogio;
        _push = push;
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

        _db.AuditLogs.Add(Auditoria.Novo(
            _db.TenantId, "FechamentoDia", row.Id, "fechar-dia", quem, _relogio.UtcAgora,
            depois: equipeId?.ToString()));
        if (equipeId is Guid eqApro)
            await ApropriarEncarregadoAsync(dia, eqApro, quem.Id, config, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var montado = await MontarAsync(dia, equipeId, cancellationToken);
        await _push.NotificarGestoresAsync(
            "Dia fechado no PREMAG",
            $"{quem.Perfil} fechou {montado.Escopo} em {dia:dd/MM}.",
            "/fechamento",
            cancellationToken);
        return montado;
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

        var vigentes = await _db.FechamentosDia
            .Where(f => f.Data == dia && f.ReabertoEm == null)
            .ToListAsync(cancellationToken);
        var rows = vigentes
            .Where(f => CalculoFechamento.EncaixaReabertura(equipeId, f.EquipeId))
            .ToList();
        if (rows.Count == 0)
            throw new RegraNegocioException("DIA_NAO_FECHADO", "Este dia não está fechado por registro.", 422);

        var agora = _relogio.UtcAgora;
        foreach (var row in rows)
        {
            row.ReabertoEm = agora;
            row.ReabertoPorId = quem.Id;
            row.MotivoReabertura = motivo;
            _db.AuditLogs.Add(Auditoria.Novo(
                _db.TenantId, "FechamentoDia", row.Id, "reabrir-dia", quem, agora,
                depois: motivo));
        }
        await _db.SaveChangesAsync(cancellationToken);
        return await MontarAsync(dia, equipeId, cancellationToken);
    }

    public async Task FecharAutomaticoDoTurnoAsync(CancellationToken cancellationToken = default)
    {
        var config = await _db.Configuracoes.FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        if (_relogio.HoraSaoPaulo < config.JornadaFim)
            return;

        var dia = _relogio.HojeSaoPaulo;
        if (CalculoFechamento.FechadoPorCalendario(dia, dia, config.DiasFechamento))
            return;

        var equipes = await _db.Equipes.Where(e => e.Ativa).ToListAsync(cancellationToken);
        var adminId = await _db.Users.AsNoTracking()
            .Where(u => u.Ativo && (u.Perfil == Permissoes.Admin || u.Perfil == Permissoes.Gerente))
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (adminId == Guid.Empty)
            return;

        foreach (var equipe in equipes)
        {
            var registros = await _db.FechamentosDia.AsNoTracking()
                .Where(f => f.Data == dia)
                .Select(f => new RegistroFechamento(f.Data, f.EquipeId, f.ReabertoEm))
                .ToListAsync(cancellationToken);
            if (CalculoFechamento.FechadoPorRegistro(dia, equipe.Id, registros))
                continue;

            var colabIds = await _db.Colaboradores.AsNoTracking()
                .Where(c => c.EquipeId == equipe.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            var abertos = await _db.Apontamentos
                .Where(a => a.Data == dia && a.HoraFim == null && colabIds.Contains(a.ColaboradorId))
                .ToListAsync(cancellationToken);
            foreach (var a in abertos)
            {
                var fim = config.JornadaFim;
                if (fim <= a.HoraInicio)
                    continue;
                if (CalculoApontamento.InteiramenteNoIntervalo(a.HoraInicio, fim, config.IntervaloInicio, config.IntervaloFim))
                    continue;
                a.HoraFim = fim;
                a.MinutosEfetivos = CalculoApontamento.MinutosEfetivos(
                    a.HoraInicio, fim, config.IntervaloInicio, config.IntervaloFim);
                a.AlteradoEm = _relogio.UtcAgora;
            }

            var row = await _db.FechamentosDia
                .FirstOrDefaultAsync(f => f.Data == dia && f.EquipeId == equipe.Id, cancellationToken);
            var por = equipe.EncarregadoId ?? adminId;
            if (row is null)
            {
                row = new FechamentoDia
                {
                    TenantId = _db.TenantId,
                    Data = dia,
                    EquipeId = equipe.Id,
                    FechadoEm = _relogio.UtcAgora,
                    FechadoPorId = por
                };
                _db.FechamentosDia.Add(row);
            }
            else if (row.ReabertoEm is not null)
            {
                row.FechadoEm = _relogio.UtcAgora;
                row.FechadoPorId = por;
                row.ReabertoEm = null;
                row.ReabertoPorId = null;
            }
            else
                continue;

            await ApropriarEncarregadoAsync(dia, equipe.Id, por, config, cancellationToken);

            _db.AuditLogs.Add(new AuditLog
            {
                TenantId = _db.TenantId,
                Entidade = "FechamentoDia",
                EntidadeId = row.Id,
                Acao = "fechar-auto",
                Depois = equipe.Nome,
                UsuarioId = por,
                Em = _relogio.UtcAgora
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
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

    private async Task ApropriarEncarregadoAsync(
        DateOnly dia,
        Guid equipeId,
        Guid criadoPorId,
        Configuracao config,
        CancellationToken cancellationToken)
    {
        var equipe = await _db.Equipes.FirstOrDefaultAsync(e => e.Id == equipeId, cancellationToken);
        if (equipe is null)
            return;

        var colabs = await _db.Colaboradores
            .Where(c => c.EquipeId == equipeId && c.Ativo)
            .ToListAsync(cancellationToken);

        var enc = colabs.FirstOrDefault(c =>
            c.Funcao.Contains("encarregado", StringComparison.OrdinalIgnoreCase));

        Usuario? user = null;
        if (equipe.EncarregadoId is Guid uid)
            user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);

        if (enc is null && user?.ColaboradorId is Guid cid)
            enc = await _db.Colaboradores.FirstOrDefaultAsync(c => c.Id == cid && c.Ativo, cancellationToken);

        if (enc is null && user is not null)
            enc = colabs.FirstOrDefault(c =>
                string.Equals(c.Nome, user.NomeExibicao, StringComparison.OrdinalIgnoreCase));

        if (enc is null && user is not null)
            enc = await CriarColaboradorEncarregadoAsync(equipe, user, cancellationToken);

        if (enc is null)
            return;

        var ja = await _db.Apontamentos
            .Where(a => a.ColaboradorId == enc.Id && a.Data == dia && a.HoraFim != null)
            .SumAsync(a => a.MinutosEfetivos ?? 0, cancellationToken);
        if (ja > 10)
            return;

        var pesos = await _db.Apontamentos
            .Where(a => a.Data == dia
                && a.HoraFim != null
                && a.MinutosEfetivos != null
                && a.Colaborador.EquipeId == equipeId
                && a.ColaboradorId != enc.Id
                && !a.Frente.Etapa.Indireta
                && !a.Frente.Obra.Interna)
            .GroupBy(a => a.FrenteId)
            .Select(g => new { FrenteId = g.Key, Peso = g.Sum(x => x.MinutosEfetivos!.Value) })
            .ToListAsync(cancellationToken);
        if (pesos.Count == 0)
            return;

        var teto = config.JornadaPadraoMinutos;
        var jornada = await _db.JornadasDia.AsNoTracking()
            .FirstOrDefaultAsync(j => j.ColaboradorId == enc.Id && j.Data == dia, cancellationToken);
        if (jornada is { MinutosApurados: > 0 })
            teto = jornada.MinutosApurados;
        teto = Math.Max(0, teto - ja);
        if (teto <= 0)
            return;

        var fatias = RateioPonderado.Distribuir(teto, pesos.Select(p => (p.FrenteId, p.Peso)).ToList());
        var janelas = RateioPonderado.EncaixarNaJornada(
            config.JornadaInicio, config.JornadaFim, config.IntervaloInicio, config.IntervaloFim, fatias);
        var agora = _relogio.UtcAgora;
        foreach (var j in janelas)
        {
            if (j.Minutos <= 0 || j.Fim <= j.Inicio)
                continue;
            _db.Apontamentos.Add(new Apontamento
            {
                TenantId = _db.TenantId,
                ClienteUuid = GeradorId.Novo(),
                ColaboradorId = enc.Id,
                FrenteId = j.Chave,
                Data = dia,
                HoraInicio = j.Inicio,
                HoraFim = j.Fim,
                MinutosEfetivos = j.Minutos,
                Observacao = "Rateio do encarregado pelas frentes do dia",
                Origem = OrigemApontamento.App,
                JornadaNaoVerificada = jornada is null,
                CriadoPorId = criadoPorId,
                CriadoEm = agora
            });
        }
    }

    private async Task<Colaborador> CriarColaboradorEncarregadoAsync(
        Equipe equipe,
        Usuario user,
        CancellationToken cancellationToken)
    {
        var matricula = (user.UserName ?? "ENC").Trim();
        if (matricula.Length > 20)
            matricula = matricula[..20];
        if (await _db.Colaboradores.AnyAsync(c => c.Matricula == matricula, cancellationToken))
            matricula = ("E" + user.Id.ToString("N")[..8]).ToUpperInvariant();

        var agora = _relogio.UtcAgora;
        var colab = new Colaborador
        {
            TenantId = _db.TenantId,
            Matricula = matricula,
            Nome = string.IsNullOrWhiteSpace(user.NomeExibicao)
                ? (string.IsNullOrWhiteSpace(user.UserName) ? "Encarregado" : user.UserName.Trim())
                : user.NomeExibicao.Trim(),
            Funcao = "Encarregado",
            EquipeId = equipe.Id,
            Ativo = true,
            OrigemCadastro = OrigemCadastro.Manual,
            CriadoEm = agora,
            AlteradoEm = agora
        };
        _db.Colaboradores.Add(colab);
        user.ColaboradorId = colab.Id;
        return colab;
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
            .Where(f => f.ReabertoEm is null && CalculoFechamento.EncaixaReabertura(equipeId, f.EquipeId))
            .OrderByDescending(f => f.EquipeId is null) // planta primeiro se ambos
            .ThenByDescending(f => f.FechadoEm)
            .FirstOrDefault();

        var reaberto = registros
            .Where(f => f.ReabertoEm is not null && CalculoFechamento.EncaixaReabertura(equipeId, f.EquipeId))
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
