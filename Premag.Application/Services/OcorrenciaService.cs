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

public class OcorrenciaService : IOcorrenciaService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;
    private readonly IPushService _push;

    public OcorrenciaService(ApplicationDbContext db, IRelogio relogio, IPushService push)
    {
        _db = db;
        _relogio = relogio;
        _push = push;
    }

    public async Task<IReadOnlyList<OcorrenciaDto>> ListarAsync(
        DateOnly? data,
        bool pendentes,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var dia = data ?? _relogio.HojeSaoPaulo;
        var query = _db.Ocorrencias.AsNoTracking()
            .Include(o => o.Colaborador)
            .Include(o => o.Frente)
            .Where(o => o.Data == dia);

        if (pendentes)
            query = query.Where(o => o.ReconhecidaEm == null);

        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            if (quem.EquipeId is null)
                return [];
            query = query.Where(o => o.EquipeId == quem.EquipeId || o.Colaborador!.EquipeId == quem.EquipeId);
        }

        var lista = await query.OrderByDescending(o => o.Severidade).ThenByDescending(o => o.DetectadaEm)
            .ToListAsync(cancellationToken);
        return lista.Select(Mapear).ToList();
    }

    public async Task ReconhecerAsync(
        Guid id,
        ReconhecerOcorrenciaDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var o = await _db.Ocorrencias
            .Include(x => x.Colaborador)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("OCORRENCIA_NAO_ENCONTRADA", "Ocorrência não encontrada.", 404);

        if (o.ReconhecidaEm is not null)
            return;

        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            if (o.Severidade == SeveridadeOcorrencia.GerenciaDiretoria)
                throw new RegraNegocioException("RN-08", "Só a gerência reconhece alerta escalonado.", 403);
            var equipe = o.EquipeId ?? o.Colaborador?.EquipeId;
            if (equipe != quem.EquipeId)
                throw new RegraNegocioException("RN-10", "Encarregado só reconhece alerta da própria equipe.", 403);
        }

        if (o.Severidade == SeveridadeOcorrencia.GerenciaDiretoria
            && string.IsNullOrWhiteSpace(dto.Justificativa))
        {
            throw new RegraNegocioException("RN-08", "Informe a justificativa para reconhecer o alerta escalonado.", 422);
        }

        o.ReconhecidaEm = _relogio.UtcAgora;
        o.ReconhecidaPorId = quem.Id;
        o.Justificativa = string.IsNullOrWhiteSpace(dto.Justificativa) ? null : dto.Justificativa.Trim();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DetectarAgoraAsync(CancellationToken cancellationToken = default)
    {
        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        var dia = _relogio.HojeSaoPaulo;
        var agora = HorarioPlanta(config);
        var (inicioDia, fimDia) = JanelaDia.Utc(dia, _relogio.AgoraSaoPaulo.Offset);

        var colaboradores = await _db.Colaboradores.AsNoTracking().ToListAsync(cancellationToken);
        var ids = colaboradores.Select(c => c.Id).ToList();
        var apontamentos = await _db.Apontamentos.AsNoTracking()
            .Include(a => a.Frente).ThenInclude(f => f.Etapa)
            .Where(a => ids.Contains(a.ColaboradorId) && a.Data == dia)
            .ToListAsync(cancellationToken);
        var jornadas = await _db.JornadasDia.AsNoTracking()
            .Where(j => ids.Contains(j.ColaboradorId) && j.Data == dia)
            .ToListAsync(cancellationToken);

        var porColab = apontamentos.GroupBy(a => a.ColaboradorId).ToDictionary(g => g.Key, g => g.ToList());
        var alertaColabs = colaboradores.Select(c =>
        {
            var lista = porColab.GetValueOrDefault(c.Id) ?? [];
            var jornada = jornadas.FirstOrDefault(j => j.ColaboradorId == c.Id);
            var situacao = jornada?.Situacao ?? (c.Ativo ? SituacaoJornada.Presente : SituacaoJornada.Afastado);
            var aberto = lista.FirstOrDefault(a => a.HoraFim is null);
            var trabalhados = lista.Where(a => a.HoraFim is not null).Sum(a => a.MinutosEfetivos ?? 0);
            var fechados = lista.Where(a => a.HoraFim is not null).Select(a => a.HoraFim!.Value).ToList();
            TimeOnly? ultimoFim = fechados.Count == 0 ? null : fechados.Max();
            return new ColaboradorAlerta(
                c.Id,
                c.EquipeId,
                c.Ativo,
                situacao,
                ultimoFim ?? config.JornadaInicio,
                aberto?.HoraInicio,
                trabalhados);
        }).ToList();

        var frentes = await _db.Frentes.AsNoTracking()
            .Include(f => f.Obra)
            .Include(f => f.Etapa)
            .Where(f => f.Ativa)
            .ToListAsync(cancellationToken);
        var producoesHoje = await _db.Producoes.AsNoTracking()
            .Where(p => p.Data == dia)
            .GroupBy(p => p.FrenteId)
            .Select(g => new { FrenteId = g.Key, Qtd = g.Sum(x => x.Quantidade) })
            .ToListAsync(cancellationToken);
        var qtdMapa = producoesHoje.ToDictionary(x => x.FrenteId, x => x.Qtd);
        var fotosSet = (await _db.Fotos.AsNoTracking()
            .Where(f => f.Tipo == TipoFoto.Avanco && f.CapturadaEm >= inicioDia && f.CapturadaEm < fimDia)
            .Select(f => f.FrenteId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var alertaFrentes = frentes.Select(f =>
        {
            var minutos = apontamentos
                .Where(a => a.FrenteId == f.Id && a.HoraFim is not null)
                .Sum(a => a.MinutosEfetivos ?? 0);
            var abertos = apontamentos.Where(a => a.FrenteId == f.Id && a.HoraFim is null);
            foreach (var a in abertos)
                minutos += CalculoApontamento.MinutosEfetivos(a.HoraInicio, agora, config.IntervaloInicio, config.IntervaloFim);
            return new FrenteAlerta(
                f.Id,
                f.Obra.Interna || f.Etapa.Indireta,
                qtdMapa.GetValueOrDefault(f.Id),
                minutos,
                fotosSet.Contains(f.Id));
        }).ToList();

        var detectados = CalculoAlertas.Detectar(
            dia, agora, config.IntervaloInicio, config.IntervaloFim,
            config.MinutosOciosidadeEscalonamento,
            config.MinutosSemServico,
            config.MinutosServicoAbertoDemais,
            alertaColabs, alertaFrentes);

        var existentes = await _db.Ocorrencias.Where(o => o.Data == dia).ToListAsync(cancellationToken);
        var novas = new List<Ocorrencia>();

        foreach (var a in detectados)
        {
            Ocorrencia? dup;
            if (a.ColaboradorId is Guid cid)
            {
                dup = existentes.FirstOrDefault(o =>
                    o.Tipo == a.Tipo && o.ColaboradorId == cid && o.JanelaInicio == a.JanelaInicio);
            }
            else
            {
                dup = existentes.FirstOrDefault(o => o.Tipo == a.Tipo && o.FrenteId == a.FrenteId);
            }

            if (dup is not null)
            {
                if (dup.ReconhecidaEm is null)
                    dup.MinutosDecorridos = a.MinutosDecorridos;
                continue;
            }

            var nova = new Ocorrencia
            {
                TenantId = _db.TenantId,
                Tipo = a.Tipo,
                Severidade = a.Severidade,
                ColaboradorId = a.ColaboradorId,
                FrenteId = a.FrenteId,
                EquipeId = a.EquipeId,
                Data = dia,
                DetectadaEm = _relogio.UtcAgora,
                JanelaInicio = a.JanelaInicio,
                MinutosDecorridos = a.MinutosDecorridos
            };
            _db.Ocorrencias.Add(nova);
            existentes.Add(nova);
            novas.Add(nova);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await NotificarNovasAsync(novas, cancellationToken);
    }

    private async Task NotificarNovasAsync(List<Ocorrencia> novas, CancellationToken cancellationToken)
    {
        if (novas.Count == 0)
            return;

        var gestores = novas.Where(o => o.Severidade == SeveridadeOcorrencia.GerenciaDiretoria).ToList();
        if (gestores.Count > 0)
        {
            var (titulo, detalhe) = Titulo(gestores[0]);
            var corpo = gestores.Count == 1 ? detalhe : $"{gestores.Count} alertas escalonados no turno.";
            await _push.NotificarGestoresAsync(titulo, corpo, "/alertas", cancellationToken);
        }

        foreach (var grupo in novas.Where(o => o.EquipeId is Guid).GroupBy(o => o.EquipeId!.Value))
        {
            var primeira = grupo.First();
            var (titulo, detalhe) = Titulo(primeira);
            var corpo = grupo.Count() == 1 ? detalhe : $"{grupo.Count()} alertas na equipe.";
            await _push.NotificarEquipeAsync(grupo.Key, titulo, corpo, "/alertas", cancellationToken);
        }
    }

    private TimeOnly HorarioPlanta(Configuracao config)
    {
        var agora = _relogio.HoraSaoPaulo;
        if (agora < config.JornadaInicio) return config.JornadaInicio;
        if (agora > config.JornadaFim) return config.JornadaFim;
        return agora;
    }

    public static OcorrenciaDto Mapear(Ocorrencia o)
    {
        var (titulo, detalhe) = Titulo(o);
        return new OcorrenciaDto
        {
            Id = o.Id,
            Tipo = o.Tipo,
            Severidade = o.Severidade,
            Titulo = titulo,
            Detalhe = detalhe,
            ColaboradorId = o.ColaboradorId,
            ColaboradorNome = o.Colaborador?.Nome,
            FrenteId = o.FrenteId,
            FrenteNome = o.Frente?.Nome,
            EquipeId = o.EquipeId,
            Data = o.Data,
            DetectadaEm = o.DetectadaEm,
            MinutosDecorridos = o.MinutosDecorridos,
            ReconhecidaEm = o.ReconhecidaEm,
            Justificativa = o.Justificativa
        };
    }

    private static (string Titulo, string Detalhe) Titulo(Ocorrencia o)
    {
        var quem = o.Colaborador?.Nome;
        var frente = o.Frente?.Nome;
        return o.Tipo switch
        {
            TipoOcorrencia.OciosidadeEscalonada => (
                "Sem alocação há mais de 1 hora",
                quem is null
                    ? "Colaborador presente e fora de frente. Escalonado para gerência e diretoria."
                    : $"{quem} presente e fora de frente. Escalonado para gerência e diretoria."),
            TipoOcorrencia.SemServico => (
                "Sem serviço aberto",
                quem is null
                    ? $"{o.MinutosDecorridos} min sem apontamento."
                    : $"{quem}: {o.MinutosDecorridos} min sem apontamento."),
            TipoOcorrencia.ServicoAbertoDemais => (
                "Serviço aberto há muito tempo",
                quem is null
                    ? "Há mais de 5 h no mesmo serviço."
                    : $"{quem} há mais de 5 h no mesmo serviço."),
            TipoOcorrencia.FrenteSemQuantidade => (
                "Frente sem quantidade",
                frente is null
                    ? "Houve apontamento nesta frente hoje e nenhuma quantidade foi lançada."
                    : $"{frente}: houve apontamento hoje e nenhuma quantidade foi lançada."),
            TipoOcorrencia.AvancoSemFoto => (
                "Avanço sem registro fotográfico",
                frente is null
                    ? "Houve quantidade apontada hoje e nenhuma foto de avanço anexada."
                    : $"{frente} teve quantidade apontada hoje e nenhuma foto anexada."),
            _ => (o.Tipo.ToString(), string.Empty)
        };
    }
}
