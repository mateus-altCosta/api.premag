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

/// <summary>Lançamento de quantidade (RN-05, RN-06b, RN-12) e índices da seção 13.</summary>
public class ProducaoService : IProducaoService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;
    private readonly IFechamentoService _fechamento;

    public ProducaoService(ApplicationDbContext db, IRelogio relogio, IFechamentoService fechamento)
    {
        _db = db;
        _relogio = relogio;
        _fechamento = fechamento;
    }

    public async Task<ProducaoResultadoDto> RegistrarAsync(
        RegistrarProducaoDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (dto.Quantidade <= 0)
            throw new RegraNegocioException("QUANTIDADE_INVALIDA", "Informe uma quantidade maior que zero.", 422);

        // RN-12: repetir o mesmo ClienteUuid devolve o lançamento já gravado.
        var existente = await _db.Producoes
            .Include(p => p.Frente)
            .FirstOrDefaultAsync(p => p.ClienteUuid == dto.ClienteUuid, cancellationToken);
        if (existente is not null)
        {
            return new ProducaoResultadoDto
            {
                Id = existente.Id,
                FrenteId = existente.FrenteId,
                Data = existente.Data,
                Quantidade = existente.Quantidade,
                QuantidadeConcluidaFrente = existente.Frente.QuantidadeConcluida
            };
        }

        var frente = await _db.Frentes
            .Include(f => f.Etapa)
            .FirstOrDefaultAsync(f => f.Id == dto.FrenteId && f.Ativa, cancellationToken)
            ?? throw new RegraNegocioException("FRENTE_NAO_ENCONTRADA", "Frente não encontrada.", 404);

        if (frente.Etapa.Indireta)
            throw new RegraNegocioException("FRENTE_INDIRETA", "Não se lança quantidade em frente de parada.", 422);

        var dia = dto.Data ?? _relogio.HojeSaoPaulo;
        await _fechamento.GarantirAbertoAsync(dia, frente.EquipeId, cancellationToken);
        var jaHoje = await _db.Producoes
            .Where(p => p.FrenteId == frente.Id && p.Data == dia)
            .SumAsync(p => p.Quantidade, cancellationToken);
        var avisos = new List<string>();
        if (jaHoje > 0)
            avisos.Add("QUANTIDADE_JA_LANCADA_HOJE"); // RN-05

        var novaConcluida = frente.QuantidadeConcluida + dto.Quantidade;
        GarantirTetoPrevisao(frente.QuantidadePrevista, novaConcluida, quem);

        var producao = new Producao
        {
            TenantId = _db.TenantId,
            FrenteId = frente.Id,
            Data = dia,
            Quantidade = dto.Quantidade,
            ApontamentoId = dto.ApontamentoId,
            RegistradoPorId = quem.Id,
            RegistradoEm = _relogio.UtcAgora,
            ClienteUuid = dto.ClienteUuid
        };
        _db.Producoes.Add(producao);
        frente.QuantidadeConcluida = novaConcluida;
        await _db.SaveChangesAsync(cancellationToken);

        return new ProducaoResultadoDto
        {
            Id = producao.Id,
            FrenteId = frente.Id,
            Data = dia,
            Quantidade = dto.Quantidade,
            QuantidadeConcluidaFrente = frente.QuantidadeConcluida,
            Avisos = avisos,
            QuantidadeJaLancadaHoje = jaHoje > 0 ? jaHoje : null
        };
    }

    public async Task<IReadOnlyList<FrenteDto>> EnriquecerComIndicesAsync(
        IReadOnlyList<FrenteDto> frentes,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (frentes.Count == 0)
            return frentes;

        var ids = frentes.Select(f => f.Id).ToList();
        var hoje = _relogio.HojeSaoPaulo;
        var verCusto = Permissoes.Tem(quem.Perfil, Permissoes.Gerente); // RN-13

        // Fator indireto é da fábrica: todos os apontamentos encerrados do tenant (seção 13).
        var apontamentos = await _db.Apontamentos.AsNoTracking()
            .Where(a => a.HoraFim != null && a.MinutosEfetivos != null)
            .Select(a => new
            {
                a.FrenteId,
                Interna = a.Frente.Etapa.Indireta,
                Minutos = a.MinutosEfetivos!.Value,
                // RN-14: custo só da folha; cadastro manual e nulo ficam de fora.
                Custo = a.Colaborador.OrigemCadastro == OrigemCadastro.Folha ? a.Colaborador.CustoHora : null,
                a.Data
            })
            .ToListAsync(cancellationToken);

        var producoes = await _db.Producoes.AsNoTracking()
            .Where(p => ids.Contains(p.FrenteId))
            .Select(p => new { p.FrenteId, p.Data, p.Quantidade })
            .ToListAsync(cancellationToken);

        var entradasFrente = frentes.Select(f => new FrenteIndice(
            f.Id,
            f.EtapaIndireta,
            f.QuantidadePrevista,
            f.QuantidadeConcluida,
            f.HhOrcadoPorUnidade,
            f.TaxaAcoKgPorUnidade)).ToList();

        var apt = apontamentos.Select(a => new ApontamentoIndice(a.FrenteId, a.Interna, a.Minutos, a.Custo, a.Data)).ToList();
        var prod = producoes.Select(p => new ProducaoIndice(p.FrenteId, p.Data, p.Quantidade)).ToList();
        var mapa = CalculoIndices.Calcular(entradasFrente, apt, prod, hoje);

        foreach (var f in frentes)
        {
            if (!mapa.TryGetValue(f.Id, out var ix))
                continue;
            f.HhDireto = ix.HhDireto;
            f.HhIndiretoRateado = ix.HhIndiretoRateado;
            f.HhTotal = ix.HhTotal;
            f.HhPorUnidade = ix.HhPorUnidade;
            f.DesvioPercentual = ix.DesvioPercentual;
            f.AmostraInsuficiente = ix.AmostraInsuficiente;
            f.AcoEstimadoKg = ix.AcoEstimadoKg; // RN-19
            f.Ritmo = ix.Ritmo;
            f.DiasParaConcluir = ix.DiasParaConcluir;
            if (verCusto)
            {
                f.CustoTotal = ix.CustoTotal;
                f.CustoPorUnidade = ix.CustoPorUnidade;
            }
        }

        return frentes;
    }

    /// <summary>RN-06b: soma não pode passar da previsão em mais de 10% sem Gerente.</summary>
    public static void GarantirTetoPrevisao(decimal prevista, decimal novaConcluida, UsuarioLogado quem)
    {
        if (!CalculoIndices.ExcedePrevisaoEmMaisDeDezPorCento(prevista, novaConcluida))
            return;
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            throw new RegraNegocioException(
                "RN-06b",
                "A quantidade ultrapassa a previsão da frente em mais de 10%. Só o Gerente pode confirmar esse lançamento.",
                422,
                new Dictionary<string, object?>
                {
                    ["quantidadePrevista"] = prevista,
                    ["quantidadeResultante"] = novaConcluida
                });
        }
    }
}
