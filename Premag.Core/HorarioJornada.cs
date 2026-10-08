using System.Globalization;
using Premag.Core.Exceptions;

namespace Premag.Core;

/// <summary>Jornada e intervalo de almoço da planta (Configuracao).</summary>
public static class HorarioJornada
{
    public static TimeOnly Interpretar(string? valor, string campo)
    {
        var t = (valor ?? "").Trim();
        if (TimeOnly.TryParseExact(
                t,
                ["HH:mm", "H:mm", "HH:mm:ss"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var hora)
            || TimeOnly.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out hora))
            return new TimeOnly(hora.Hour, hora.Minute);
        throw new RegraNegocioException("HORARIO_INVALIDO", $"{campo} deve estar no formato HH:mm.");
    }

    public static void Validar(TimeOnly inicio, TimeOnly fim, TimeOnly intervaloInicio, TimeOnly intervaloFim)
    {
        if (inicio >= fim)
            throw new RegraNegocioException("JORNADA_INVALIDA", "O fim da jornada deve ser depois do início.");
        if (intervaloInicio >= intervaloFim)
            throw new RegraNegocioException("INTERVALO_INVALIDO", "O fim do almoço deve ser depois do início.");
        if (intervaloInicio < inicio || intervaloFim > fim)
            throw new RegraNegocioException("INTERVALO_FORA", "O almoço precisa ficar dentro da jornada.");
    }

    public static int MinutosEfetivos(TimeOnly inicio, TimeOnly fim, TimeOnly intervaloInicio, TimeOnly intervaloFim)
    {
        Validar(inicio, fim, intervaloInicio, intervaloFim);
        var bruto = (int)(fim - inicio).TotalMinutes;
        var intervalo = (int)(intervaloFim - intervaloInicio).TotalMinutes;
        return Math.Max(0, bruto - intervalo);
    }
}
