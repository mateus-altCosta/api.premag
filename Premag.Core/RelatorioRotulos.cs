namespace Premag.Core;

/// <summary>Rótulos do relatório da Diretoria: HH/m³ (não HH/un) e Un. Est. com unidade.</summary>
public static class RelatorioRotulos
{
    public static string HhPorUnidade(string? unidade)
    {
        var u = string.IsNullOrWhiteSpace(unidade) ? "un" : unidade.Trim();
        return $"HH/{u}";
    }

    /// <summary>
    /// Un. Est.: aço com unidade da taxa (kg na armação) ou quantidade feita na unidade da frente (m³, m², m).
    /// </summary>
    public static (decimal Valor, string Unidade) UnidadeEstimada(
        decimal? acoEstimado,
        string? unidadeAco,
        decimal quantidadeConcluida,
        string? unidadeFrente)
    {
        if (acoEstimado is decimal aco)
            return (aco, string.IsNullOrWhiteSpace(unidadeAco) ? "kg" : unidadeAco.Trim());
        return (
            quantidadeConcluida,
            string.IsNullOrWhiteSpace(unidadeFrente) ? "un" : unidadeFrente.Trim());
    }
}
