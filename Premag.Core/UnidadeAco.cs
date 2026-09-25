namespace Premag.Core;

/// <summary>Unidade da taxa de aço da frente (kg, m² ou m³ por unidade da frente).</summary>
public static class UnidadeAco
{
    public static readonly string[] Permitidas = ["kg", "m²", "m³"];

    public static string Normalizar(string? valor)
    {
        var v = (valor ?? "").Trim().ToLowerInvariant()
            .Replace("m2", "m²")
            .Replace("m3", "m³")
            .Replace("m^2", "m²")
            .Replace("m^3", "m³");
        if (v is "kg" or "m²" or "m³")
            return v;
        return "kg";
    }

    public static bool EhValida(string? valor) =>
        Permitidas.Contains(Normalizar(valor));
}
