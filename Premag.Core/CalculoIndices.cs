namespace Premag.Core;

/// <summary>Entradas puras para o cálculo da seção 13.</summary>
public sealed record ApontamentoIndice(
    Guid FrenteId,
    bool Interna,
    int MinutosEfetivos,
    decimal? CustoHora,
    DateOnly Data);

public sealed record ProducaoIndice(Guid FrenteId, DateOnly Data, decimal Quantidade);

public sealed record FrenteIndice(
    Guid Id,
    bool Interna,
    decimal QuantidadePrevista,
    decimal QuantidadeConcluida,
    decimal? HhOrcadoPorUnidade,
    decimal? TaxaAcoKgPorUnidade);

public sealed class IndiceFrente
{
    public Guid FrenteId { get; init; }
    public decimal HhDireto { get; init; }
    public decimal HhIndiretoRateado { get; init; }
    public decimal HhTotal { get; init; }
    public decimal Quantidade { get; init; }
    public decimal? HhPorUnidade { get; init; }
    public decimal? DesvioPercentual { get; init; }
    public bool AmostraInsuficiente { get; init; }
    public int LancamentosProducao { get; init; }
    public decimal? CustoDireto { get; init; }
    public decimal? CustoTotal { get; init; }
    public decimal? CustoPorUnidade { get; init; }
    public decimal AcoEstimadoKg { get; init; }
    public decimal AvancoPercentual { get; init; }
    public decimal Ritmo { get; init; }
    public int? DiasParaConcluir { get; init; }
}

/// <summary>Cálculo de índices realizados (memorial seção 13).</summary>
public static class CalculoIndices
{
    public const int MinimoLancamentosAmostra = 5;
    public const int DiasUteisRitmo = 22;
    public const decimal FolgaPrevisao = 1.10m;

    public static decimal Hh(int minutos) => minutos / 60m;

    /// <summary>RN-06b: soma não pode passar da previsão em mais de 10% sem Gerente.</summary>
    public static bool ExcedePrevisaoEmMaisDeDezPorCento(decimal prevista, decimal concluidaMaisNova) =>
        prevista > 0 && concluidaMaisNova > prevista * FolgaPrevisao;

    public static IReadOnlyDictionary<Guid, IndiceFrente> Calcular(
        IReadOnlyList<FrenteIndice> frentes,
        IReadOnlyList<ApontamentoIndice> apontamentos,
        IReadOnlyList<ProducaoIndice> producoes,
        DateOnly ate)
    {
        var hhDiretoPorFrente = apontamentos
            .Where(a => !a.Interna)
            .GroupBy(a => a.FrenteId)
            .ToDictionary(g => g.Key, g => Hh(g.Sum(x => x.MinutosEfetivos)));

        var hhDiretoTotal = hhDiretoPorFrente.Values.Sum();
        var hhIndireto = Hh(apontamentos.Where(a => a.Interna).Sum(a => a.MinutosEfetivos));
        var fator = hhDiretoTotal == 0 ? 0 : hhIndireto / hhDiretoTotal;

        var custoDiretoPorFrente = apontamentos
            .Where(a => !a.Interna && a.CustoHora is not null)
            .GroupBy(a => a.FrenteId)
            .ToDictionary(g => g.Key, g => g.Sum(x => Hh(x.MinutosEfetivos) * x.CustoHora!.Value));

        var prodPorFrente = producoes.GroupBy(p => p.FrenteId).ToDictionary(g => g.Key, g => g.ToList());
        var inicioRitmo = ate.AddDays(1 - DiasUteisRitmo);
        var mapa = new Dictionary<Guid, IndiceFrente>();

        foreach (var frente in frentes)
        {
            var hhDireto = hhDiretoPorFrente.GetValueOrDefault(frente.Id);
            var hhTotal = frente.Interna ? hhDireto : hhDireto * (1 + fator);
            var hhIndiretoRateado = frente.Interna ? 0 : hhTotal - hhDireto;
            var lancamentos = prodPorFrente.GetValueOrDefault(frente.Id) ?? [];
            var quantidade = lancamentos.Sum(p => p.Quantidade);
            var hhPorUnidade = quantidade == 0 ? (decimal?)null : hhTotal / quantidade;
            decimal? desvio = null;
            if (hhPorUnidade is decimal real && frente.HhOrcadoPorUnidade is decimal orc and not 0)
                desvio = (real - orc) / orc * 100;

            decimal? custoDireto = custoDiretoPorFrente.TryGetValue(frente.Id, out var cd) ? cd : null;
            decimal? custoTotal = custoDireto is null
                ? null
                : frente.Interna
                    ? custoDireto
                    : custoDireto * (1 + fator);
            decimal? custoPorUnidade = quantidade == 0 || custoTotal is null ? null : custoTotal / quantidade;

            var qtdRitmo = lancamentos.Where(p => p.Data >= inicioRitmo && p.Data <= ate).Sum(p => p.Quantidade);
            var ritmo = qtdRitmo / DiasUteisRitmo;
            var restante = frente.QuantidadePrevista - frente.QuantidadeConcluida;
            int? dias = ritmo > 0 && restante > 0
                ? (int)Math.Ceiling((double)(restante / ritmo))
                : null;

            var prevista = frente.QuantidadePrevista;
            mapa[frente.Id] = new IndiceFrente
            {
                FrenteId = frente.Id,
                HhDireto = Decimal.Round(hhDireto, 2),
                HhIndiretoRateado = Decimal.Round(hhIndiretoRateado, 2),
                HhTotal = Decimal.Round(hhTotal, 2),
                Quantidade = quantidade,
                HhPorUnidade = hhPorUnidade is decimal hpu ? Decimal.Round(hpu, 4) : null,
                DesvioPercentual = desvio is decimal d ? Decimal.Round(d, 1) : null,
                AmostraInsuficiente = lancamentos.Count < MinimoLancamentosAmostra,
                LancamentosProducao = lancamentos.Count,
                CustoDireto = custoDireto is decimal cdir ? Decimal.Round(cdir, 2) : null,
                CustoTotal = custoTotal is decimal ct ? Decimal.Round(ct, 2) : null,
                CustoPorUnidade = custoPorUnidade is decimal cpu ? Decimal.Round(cpu, 2) : null,
                // RN-19: aço é estimado (taxa de projeto × quantidade concluída).
                AcoEstimadoKg = Decimal.Round((frente.TaxaAcoKgPorUnidade ?? 0) * frente.QuantidadeConcluida, 1),
                AvancoPercentual = prevista <= 0 ? 0 : Decimal.Round(frente.QuantidadeConcluida / prevista * 100, 1),
                Ritmo = Decimal.Round(ritmo, 3),
                DiasParaConcluir = dias
            };
        }

        return mapa;
    }
}
