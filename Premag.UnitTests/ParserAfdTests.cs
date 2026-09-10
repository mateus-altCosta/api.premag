using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class ParserAfdTests
{
    [Fact]
    public void Tipo3_ExtraiPisDataHora()
    {
        // NSR(9) + tipo 3 + 27/08/2026 + 07:02 + PIS 12345678901
        var linha = "000000001327082026070212345678901";
        var m = ParserAfd.Ler(linha).Should().ContainSingle().Subject;
        m.Data.Should().Be(new DateOnly(2026, 8, 27));
        m.Hora.Should().Be(new TimeOnly(7, 2));
        m.Documento.Should().Contain("12345678901");
    }

    [Fact]
    public void LinhaCurta_Ignora()
    {
        ParserAfd.Ler("abc").Should().BeEmpty();
    }
}
