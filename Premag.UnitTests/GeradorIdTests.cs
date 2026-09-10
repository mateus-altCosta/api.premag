using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class GeradorIdTests
{
    [Fact]
    public void Novo_GeraGuidVersao7()
    {
        var id = GeradorId.Novo();
        var bytes = id.ToByteArray(bigEndian: true);
        (bytes[6] >> 4).Should().Be(7);
    }

    [Fact]
    public void Novo_GeraIdentificadoresDistintos()
    {
        GeradorId.Novo().Should().NotBe(GeradorId.Novo());
    }
}
