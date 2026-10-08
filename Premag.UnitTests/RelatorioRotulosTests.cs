using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class RelatorioRotulosTests
{
    [Theory]
    [InlineData("m³", "HH/m³")]
    [InlineData("kg", "HH/kg")]
    [InlineData("m²", "HH/m²")]
    [InlineData("m", "HH/m")]
    [InlineData("", "HH/un")]
    public void HhPorUnidade_UsaUnidadeDaFrente(string unidade, string esperado)
    {
        RelatorioRotulos.HhPorUnidade(unidade).Should().Be(esperado);
    }

    [Fact]
    public void UnidadeEstimada_ArmacaoUsaAcoEmKg()
    {
        var (valor, unidade) = RelatorioRotulos.UnidadeEstimada(11800m, "kg", 38, "pç");
        valor.Should().Be(11800m);
        unidade.Should().Be("kg");
    }

    [Theory]
    [InlineData(46, "m³", 46, "m³")]
    [InlineData(200, "m²", 200, "m²")]
    [InlineData(15, "m", 15, "m")]
    public void UnidadeEstimada_SemTaxaUsaQuantidadeDaFrente(
        decimal feito,
        string unidadeFrente,
        decimal valorEsperado,
        string unidadeEsperada)
    {
        var (valor, unidade) = RelatorioRotulos.UnidadeEstimada(null, "kg", feito, unidadeFrente);
        valor.Should().Be(valorEsperado);
        unidade.Should().Be(unidadeEsperada);
    }
}
