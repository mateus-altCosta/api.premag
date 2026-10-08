using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class EtapaPorEquipeTests
{
    private static readonly Guid Forma = Guid.Parse("11111111-1111-7111-8111-111111111121");
    private static readonly Guid Armacao = Guid.Parse("11111111-1111-7111-8111-111111111122");
    private static readonly Guid Concreto = Guid.Parse("11111111-1111-7111-8111-111111111124");
    private static readonly Guid Parada = Guid.Parse("11111111-1111-7111-8111-111111111130");

    private static readonly (Guid Id, string Nome, bool Indireta)[] Etapas =
    [
        (Forma, "Fôrma", false),
        (Armacao, "Armação", false),
        (Concreto, "Concretagem", false),
        (Parada, "Parada", true)
    ];

    [Theory]
    [InlineData("ARMAÇÃO", "11111111-1111-7111-8111-111111111122")]
    [InlineData("FÔRMAS", "11111111-1111-7111-8111-111111111121")]
    [InlineData("CONCRETO", "11111111-1111-7111-8111-111111111124")]
    public void ResolvePeloNomeDaEquipe(string equipe, string etapaId)
    {
        EtapaPorEquipe.Resolver(equipe, Etapas).Should().Be(Guid.Parse(etapaId));
    }

    [Fact]
    public void SemEquipe_UsaPrimeiraDireta()
    {
        EtapaPorEquipe.Resolver(null, Etapas).Should().Be(Forma);
    }
}
