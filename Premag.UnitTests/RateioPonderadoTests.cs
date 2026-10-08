using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class RateioPonderadoTests
{
    [Fact]
    public void Distribuir_SomaIgualAoTotal()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var fatias = RateioPonderado.Distribuir(528, [(a, 60), (b, 40)]);
        fatias.Sum(f => f.Minutos).Should().Be(528);
        fatias.Single(f => f.Chave == a).Minutos.Should().Be(317);
        fatias.Single(f => f.Chave == b).Minutos.Should().Be(211);
    }

    [Fact]
    public void EncaixarNaJornada_PulaAlmoco()
    {
        var f = Guid.NewGuid();
        var janelas = RateioPonderado.EncaixarNaJornada(
            new TimeOnly(7, 0),
            new TimeOnly(16, 48),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            [(f, 528)]);

        janelas.Sum(j => j.Minutos).Should().Be(528);
        janelas.Should().Contain(j => j.Fim == new TimeOnly(12, 0));
        janelas.Should().Contain(j => j.Inicio == new TimeOnly(13, 0));
    }
}
