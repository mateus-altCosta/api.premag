using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class AvancoObraTests
{
    [Fact]
    public void Percentual_SoContaConcretagem()
    {
        var frentes = new (bool, bool, string?, decimal, decimal)[]
        {
            (true, false, "Fôrma", 10, 10),
            (true, false, "Armação", 10, 8),
            (true, false, "Concretagem", 20, 5),
        };

        AvancoObra.Percentual(frentes).Should().Be(25m);
    }

    [Fact]
    public void Percentual_SemConcretagem_Zero()
    {
        var frentes = new (bool, bool, string?, decimal, decimal)[]
        {
            (true, false, "Armação", 10, 10),
        };
        AvancoObra.Percentual(frentes).Should().Be(0m);
    }

    [Fact]
    public void Percentual_IgnoraInativaEIndireta()
    {
        var frentes = new (bool, bool, string?, decimal, decimal)[]
        {
            (false, false, "Concretagem", 10, 10),
            (true, true, "Concretagem", 10, 10),
            (true, false, "Concretagem", 10, 4),
        };
        AvancoObra.Percentual(frentes).Should().Be(40m);
    }
}
