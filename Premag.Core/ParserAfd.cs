namespace Premag.Core;

/// <summary>Marcação de ponto extraída do Arquivo Fonte de Dados (AFD).</summary>
public sealed record MarcacaoAfd(string Documento, DateOnly Data, TimeOnly Hora);

/// <summary>
/// Leitura de AFD (REP). Aceita registro tipo 3 ou 4 após NSR de 9 dígitos:
/// data ddMMyyyy, hora HHmm e PIS/CPF (11–12 dígitos).
/// </summary>
public static class ParserAfd
{
    public static IReadOnlyList<MarcacaoAfd> Ler(string texto)
    {
        var lista = new List<MarcacaoAfd>();
        if (string.IsNullOrWhiteSpace(texto))
            return lista;

        foreach (var bruta in texto.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var linha = bruta.Trim();
            if (linha.Length < 33)
                continue;
            if (!char.IsDigit(linha[0]))
                continue;

            var tipo = linha.Length > 9 ? linha[9] : '\0';
            if (tipo is not ('3' or '4'))
                continue;

            var dataBruta = linha.Substring(10, 8);
            var horaBruta = linha.Substring(18, 4);
            if (!int.TryParse(dataBruta.AsSpan(0, 2), out var d)
                || !int.TryParse(dataBruta.AsSpan(2, 2), out var mo)
                || !int.TryParse(dataBruta.AsSpan(4, 4), out var y)
                || !int.TryParse(horaBruta.AsSpan(0, 2), out var h)
                || !int.TryParse(horaBruta.AsSpan(2, 2), out var mi))
                continue;

            try
            {
                var data = new DateOnly(y, mo, d);
                var hora = new TimeOnly(h, mi);
                var resto = linha[22..];
                var doc = new string(resto.Where(char.IsDigit).Take(12).ToArray());
                if (doc.Length is < 11 or > 12)
                    continue;
                lista.Add(new MarcacaoAfd(doc.TrimStart('0').Length == 0 ? doc : doc, data, hora));
            }
            catch (ArgumentOutOfRangeException)
            {
                /* linha inválida */
            }
        }

        return lista;
    }
}
