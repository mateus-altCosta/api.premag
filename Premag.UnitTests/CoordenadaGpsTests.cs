using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class CoordenadaGpsTests
{
    [Fact]
    public void ArredondaSeisCasas_EMantemBrasil()
    {
        var (lat, lng) = CoordenadaGps.Normalizar(-23.550519888476562m, -46.6333091234567m);
        lat.Should().Be(-23.550520m);
        lng.Should().Be(-46.633309m);
    }

    [Fact]
    public void PontoComoMilhar_NaoEstoura_Descarta()
    {
        var (lat, lng) = CoordenadaGps.Normalizar(-23550519m, -46633309m);
        lat.Should().BeNull();
        lng.Should().BeNull();
    }

    [Fact]
    public void NuloPermaneceNulo()
    {
        var (lat, lng) = CoordenadaGps.Normalizar(null, null);
        lat.Should().BeNull();
        lng.Should().BeNull();
    }
}
