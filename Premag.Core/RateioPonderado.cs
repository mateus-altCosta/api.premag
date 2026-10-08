namespace Premag.Core;

/// <summary>Rateia minutos do encarregado nas frentes na proporção do HH da equipe no dia.</summary>
public static class RateioPonderado
{
    public static IReadOnlyList<(T Chave, int Minutos)> Distribuir<T>(
        int total,
        IReadOnlyList<(T Chave, int Peso)> pesos) where T : notnull
    {
        if (total <= 0 || pesos.Count == 0)
            return [];

        var validos = pesos.Where(p => p.Peso > 0).ToList();
        var soma = validos.Sum(p => p.Peso);
        if (soma <= 0)
            return [];

        var linhas = validos.Select(p =>
        {
            var exact = (decimal)total * p.Peso / soma;
            var floor = (int)decimal.Floor(exact);
            return (p.Chave, Minutos: floor, Resto: exact - floor);
        }).ToList();

        var falta = total - linhas.Sum(l => l.Minutos);
        var ordem = linhas
            .Select((l, i) => (l.Resto, i))
            .OrderByDescending(x => x.Resto)
            .ThenBy(x => x.i)
            .Select(x => x.i)
            .ToList();
        for (var k = 0; k < falta; k++)
        {
            var idx = ordem[k % ordem.Count];
            var l = linhas[idx];
            linhas[idx] = (l.Chave, l.Minutos + 1, l.Resto);
        }

        return linhas.Where(l => l.Minutos > 0).Select(l => (l.Chave, l.Minutos)).ToList();
    }

    public static IReadOnlyList<(T Chave, TimeOnly Inicio, TimeOnly Fim, int Minutos)> EncaixarNaJornada<T>(
        TimeOnly jornadaInicio,
        TimeOnly jornadaFim,
        TimeOnly intervaloInicio,
        TimeOnly intervaloFim,
        IReadOnlyList<(T Chave, int Minutos)> fatias)
    {
        var resultado = new List<(T, TimeOnly, TimeOnly, int)>();
        var cursor = CalculoApontamento.ParaMinutos(jornadaInicio);
        var fimJ = CalculoApontamento.ParaMinutos(jornadaFim);
        var almoIni = CalculoApontamento.ParaMinutos(intervaloInicio);
        var almoFim = CalculoApontamento.ParaMinutos(intervaloFim);

        foreach (var fatia in fatias)
        {
            var rest = fatia.Minutos;
            while (rest > 0 && cursor < fimJ)
            {
                if (cursor >= almoIni && cursor < almoFim)
                {
                    cursor = almoFim;
                    continue;
                }

                var tetoSeg = cursor < almoIni ? Math.Min(almoIni, fimJ) : fimJ;
                var cabem = tetoSeg - cursor;
                if (cabem <= 0)
                {
                    cursor = tetoSeg == almoIni ? almoFim : fimJ;
                    continue;
                }

                var usar = Math.Min(rest, cabem);
                resultado.Add((
                    fatia.Chave,
                    CalculoApontamento.DeMinutos(cursor),
                    CalculoApontamento.DeMinutos(cursor + usar),
                    usar));
                cursor += usar;
                rest -= usar;
            }
        }

        return resultado;
    }
}
