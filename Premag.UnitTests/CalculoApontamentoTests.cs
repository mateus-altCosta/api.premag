using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class CalculoApontamentoTests
{
    private static readonly TimeOnly AlmocoIni = new(12, 0);
    private static readonly TimeOnly AlmocoFim = new(13, 0);

    [Fact]
    public void RN06_IntervaloRefeicaoNaoEntraNosMinutosEfetivos()
    {
        var minutos = CalculoApontamento.MinutosEfetivos(new TimeOnly(7, 0), new TimeOnly(16, 48), AlmocoIni, AlmocoFim);
        minutos.Should().Be(528);
    }

    [Fact]
    public void RN06_ApontamentoInteiroNoAlmocoERejeitado()
    {
        CalculoApontamento.InteiramenteNoIntervalo(new TimeOnly(12, 0), new TimeOnly(13, 0), AlmocoIni, AlmocoFim)
            .Should().BeTrue();
        CalculoApontamento.InteiramenteNoIntervalo(new TimeOnly(11, 50), new TimeOnly(12, 10), AlmocoIni, AlmocoFim)
            .Should().BeFalse();
    }

    [Fact]
    public void RN01_NaoPermiteApontarAlemDaJornadaApurada()
    {
        // 520 já apontados + 40 novos = 560; teto 528 → excedente 32 (cenário 17.2).
        CalculoApontamento.ExcedenteJornada(520, 40, 528).Should().Be(32);
        CalculoApontamento.ExcedenteJornada(500, 20, 528).Should().Be(0);
    }

    [Fact]
    public void RN04_IntervaloExigeMotivoQuandoGapPassaDeDezMinutos()
    {
        var gap = CalculoApontamento.MinutosGap(new TimeOnly(10, 0), new TimeOnly(10, 40), AlmocoIni, AlmocoFim);
        gap.Should().Be(40);
        CalculoApontamento.ExigeMotivoParada(gap).Should().BeTrue();
        CalculoApontamento.ExigeMotivoParada(10).Should().BeFalse();
        CalculoApontamento.ExigeMotivoParada(11).Should().BeTrue();
    }

    [Fact]
    public void RN04_AlmocoNaoContaComoGapDeParada()
    {
        var gap = CalculoApontamento.MinutosGap(new TimeOnly(11, 55), new TimeOnly(13, 5), AlmocoIni, AlmocoFim);
        gap.Should().Be(10);
        CalculoApontamento.ExigeMotivoParada(gap).Should().BeFalse();
    }

    [Fact]
    public void RN06_ServicoCruzandoAlmocoDescontaSessentaMinutos()
    {
        CalculoApontamento.MinutosEfetivos(new TimeOnly(11, 0), new TimeOnly(14, 0), AlmocoIni, AlmocoFim)
            .Should().Be(120);
    }

    [Fact]
    public void RN03_EncadearNoMesmoHorarioNaoGeraGap()
    {
        var gap = CalculoApontamento.MinutosGap(new TimeOnly(10, 30), new TimeOnly(10, 30), AlmocoIni, AlmocoFim);
        gap.Should().Be(0);
        CalculoApontamento.ExigeMotivoParada(gap).Should().BeFalse();
    }
}
