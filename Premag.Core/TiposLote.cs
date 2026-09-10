namespace Premag.Core;

/// <summary>Tipos aceitos em POST /api/sync/lote (operações do chão de fábrica).</summary>
public static class TiposLote
{
    public const string Iniciar = "iniciar";
    public const string Encerrar = "encerrar";
    public const string Producao = "producao";

    public static bool EhValido(string? tipo) =>
        tipo is not null &&
        (tipo.Equals(Iniciar, StringComparison.OrdinalIgnoreCase)
         || tipo.Equals(Encerrar, StringComparison.OrdinalIgnoreCase)
         || tipo.Equals(Producao, StringComparison.OrdinalIgnoreCase));

    public static string Normalizar(string tipo) => tipo.Trim().ToLowerInvariant();
}
