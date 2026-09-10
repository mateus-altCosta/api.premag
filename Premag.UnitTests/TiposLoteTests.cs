using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class TiposLoteTests
{
    [Theory]
    [InlineData("iniciar")]
    [InlineData("ENCERRAR")]
    [InlineData("producao")]
    public void TiposConhecidos_SaoValidos(string tipo)
    {
        TiposLote.EhValido(tipo).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("foto")]
    [InlineData("cadastro")]
    public void TiposDesconhecidos_SaoInvalidos(string? tipo)
    {
        TiposLote.EhValido(tipo).Should().BeFalse();
    }
}
