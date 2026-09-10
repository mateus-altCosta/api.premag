namespace Premag.Core;

/// <summary>
/// Cálculos de jornada e apontamento (seção 13 e RN-01, RN-04, RN-06).
/// Horários em minutos desde meia-noite, fuso da planta.
/// </summary>
public static class CalculoApontamento
{
    public const int LimiteGapMotivoMinutos = 10;

    public static int ParaMinutos(TimeOnly hora) => hora.Hour * 60 + hora.Minute;

    public static TimeOnly DeMinutos(int minutos)
    {
        var m = Math.Clamp(minutos, 0, 24 * 60 - 1);
        return new TimeOnly(m / 60, m % 60);
    }

    /// <summary>RN-06: duração descontando a interseção com o intervalo de refeição.</summary>
    public static int MinutosEfetivos(
        TimeOnly inicio,
        TimeOnly fim,
        TimeOnly intervaloInicio,
        TimeOnly intervaloFim)
    {
        var ini = ParaMinutos(inicio);
        var f = ParaMinutos(fim);
        var bruto = Math.Max(0, f - ini);
        var overlap = Intersecao(ini, f, ParaMinutos(intervaloInicio), ParaMinutos(intervaloFim));
        return Math.Max(0, bruto - overlap);
    }

    /// <summary>RN-06: apontamento inteiramente contido no intervalo não é apontável.</summary>
    public static bool InteiramenteNoIntervalo(
        TimeOnly inicio,
        TimeOnly fim,
        TimeOnly intervaloInicio,
        TimeOnly intervaloFim)
    {
        var ini = ParaMinutos(inicio);
        var f = ParaMinutos(fim);
        var a = ParaMinutos(intervaloInicio);
        var b = ParaMinutos(intervaloFim);
        return ini >= a && f <= b && f > ini;
    }

    /// <summary>RN-04: gap após o último término (ou início da jornada), já sem o almoço.</summary>
    public static int MinutosGap(
        TimeOnly origem,
        TimeOnly destino,
        TimeOnly intervaloInicio,
        TimeOnly intervaloFim) =>
        MinutosEfetivos(origem, destino, intervaloInicio, intervaloFim);

    public static bool ExigeMotivoParada(int minutosGap) => minutosGap > LimiteGapMotivoMinutos;

    /// <summary>RN-01: minutos acima do teto da jornada apurada.</summary>
    public static int ExcedenteJornada(int minutosJaApontados, int minutosNovos, int tetoMinutos) =>
        Math.Max(0, minutosJaApontados + minutosNovos - tetoMinutos);

    public static int Intersecao(int ini, int fim, int a, int b) =>
        Math.Max(0, Math.Min(fim, b) - Math.Max(ini, a));
}
