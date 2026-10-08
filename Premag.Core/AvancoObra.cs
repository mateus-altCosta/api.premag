namespace Premag.Core;

/// <summary>
/// % da obra considera só peça concretada (etapa Concretagem), não fôrma nem armação.
/// </summary>
public static class AvancoObra
{
    public const string EtapaConcretagem = "Concretagem";

    public static bool EhPecaConcretada(string? etapaNome, bool etapaIndireta) =>
        !etapaIndireta
        && string.Equals((etapaNome ?? "").Trim(), EtapaConcretagem, StringComparison.OrdinalIgnoreCase);

    public static decimal Percentual(
        IEnumerable<(bool Ativa, bool EtapaIndireta, string? EtapaNome, decimal Prevista, decimal Concluida)> frentes)
    {
        var pecas = frentes
            .Where(f => f.Ativa && EhPecaConcretada(f.EtapaNome, f.EtapaIndireta))
            .ToList();
        var prevista = pecas.Sum(f => f.Prevista);
        var feita = pecas.Sum(f => f.Concluida);
        return prevista <= 0 ? 0 : decimal.Round(feita / prevista * 100, 1);
    }
}
