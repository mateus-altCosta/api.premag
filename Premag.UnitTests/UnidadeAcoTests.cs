using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class UnidadeAcoTests
{
    [Theory]
    [InlineData("kg", "kg")]
    [InlineData("m2", "m²")]
    [InlineData("m³", "m³")]
    [InlineData("M3", "m³")]
    [InlineData("", "kg")]
    [InlineData("litro", "kg")]
    public void Normalizar_AceitaKgM2M3(string entrada, string esperado)
    {
        UnidadeAco.Normalizar(entrada).Should().Be(esperado);
    }
}
