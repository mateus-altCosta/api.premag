using FluentAssertions;
using Premag.Core;
using Premag.Core.Exceptions;

namespace Premag.UnitTests;

public class HorarioJornadaTests
{
    [Fact]
    public void MinutosEfetivos_PadraoPremag_528()
    {
        HorarioJornada.MinutosEfetivos(
            new TimeOnly(7, 0), new TimeOnly(16, 48),
            new TimeOnly(12, 0), new TimeOnly(13, 0)).Should().Be(528);
    }

    [Fact]
    public void Validar_AlmocoForaDaJornada_Lanca()
    {
        var act = () => HorarioJornada.Validar(
            new TimeOnly(7, 0), new TimeOnly(16, 0),
            new TimeOnly(16, 30), new TimeOnly(17, 0));
        act.Should().Throw<RegraNegocioException>().Which.Codigo.Should().Be("INTERVALO_FORA");
    }

    [Theory]
    [InlineData("07:00", 7, 0)]
    [InlineData("16:48", 16, 48)]
    public void Interpretar_AceitaHHmm(string texto, int hora, int minuto)
    {
        HorarioJornada.Interpretar(texto, "Jornada").Should().Be(new TimeOnly(hora, minuto));
    }
}
