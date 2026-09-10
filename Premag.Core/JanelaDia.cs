namespace Premag.Core;

/// <summary>Npgsql só grava timestamptz com offset 0 (UTC).</summary>
public static class JanelaDia
{
    public static (DateTimeOffset InicioUtc, DateTimeOffset FimUtc) Utc(DateOnly dia, TimeSpan offsetPlanta)
    {
        var inicio = new DateTimeOffset(dia.ToDateTime(TimeOnly.MinValue), offsetPlanta).ToUniversalTime();
        return (inicio, inicio.AddDays(1));
    }
}
