using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class JanelaDiaTests
{
    [Fact]
    public void Utc_ConverteMeiaNoiteDeBrasiliaParaOffsetZero()
    {
        var (inicio, fim) = JanelaDia.Utc(new DateOnly(2026, 9, 10), TimeSpan.FromHours(-3));
        inicio.Offset.Should().Be(TimeSpan.Zero);
        inicio.Should().Be(new DateTimeOffset(2026, 9, 10, 3, 0, 0, TimeSpan.Zero));
        fim.Should().Be(inicio.AddDays(1));
    }
}
