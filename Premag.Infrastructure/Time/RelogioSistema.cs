using Premag.Core.Interfaces;

namespace Premag.Infrastructure.Time;

public sealed class RelogioSistema : IRelogio
{
    private static readonly TimeZoneInfo Fuso = Resolver();

    public DateTimeOffset UtcAgora => DateTimeOffset.UtcNow;
    public DateTimeOffset AgoraSaoPaulo => TimeZoneInfo.ConvertTime(UtcAgora, Fuso);
    public DateOnly HojeSaoPaulo => DateOnly.FromDateTime(AgoraSaoPaulo.DateTime);
    public TimeOnly HoraSaoPaulo => TimeOnly.FromDateTime(AgoraSaoPaulo.DateTime);

    private static TimeZoneInfo Resolver()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }
}
