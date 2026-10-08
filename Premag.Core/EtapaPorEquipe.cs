using System.Globalization;
using System.Text;

namespace Premag.Core;

/// <summary>Infere a etapa da frente pelo nome da equipe (esconde o campo na ficha).</summary>
public static class EtapaPorEquipe
{
    public static Guid? Resolver(
        string? equipeNome,
        IEnumerable<(Guid Id, string Nome, bool Indireta)> etapas)
    {
        var lista = etapas.Where(e => !e.Indireta).ToList();
        if (lista.Count == 0)
            return null;

        var chave = Normalizar(equipeNome);
        if (chave.Length == 0)
            return lista[0].Id;

        foreach (var trecho in new[] { "concret", "armac", "form", "protens" })
        {
            if (!chave.Contains(trecho, StringComparison.Ordinal))
                continue;
            var hit = lista.FirstOrDefault(e => Normalizar(e.Nome).Contains(trecho, StringComparison.Ordinal));
            if (hit.Id != Guid.Empty)
                return hit.Id;
        }

        foreach (var e in lista)
        {
            var en = Normalizar(e.Nome);
            if (en.Length > 0 && (chave.Contains(en, StringComparison.Ordinal) || en.Contains(chave, StringComparison.Ordinal)))
                return e.Id;
        }

        return lista[0].Id;
    }

    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "";
        var form = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var c in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
