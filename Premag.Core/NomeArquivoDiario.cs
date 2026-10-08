namespace Premag.Core;

public static class NomeArquivoDiario
{
    public static string Pdf(string? escopo, DateOnly data)
    {
        var equipe = Sanitizar(escopo);
        if (equipe.Length == 0
            || equipe.Contains("consolidado", StringComparison.OrdinalIgnoreCase)
            || equipe.Equals("Planta", StringComparison.OrdinalIgnoreCase))
            equipe = "todas as equipes";
        return $"Diário {equipe} {data:dd-MM-yyyy}.PDF";
    }

    public static string Sanitizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "";
        var invalidos = Path.GetInvalidFileNameChars();
        var limpo = new string(valor.Trim().Select(c => invalidos.Contains(c) ? ' ' : c).ToArray());
        return string.Join(' ', limpo.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
