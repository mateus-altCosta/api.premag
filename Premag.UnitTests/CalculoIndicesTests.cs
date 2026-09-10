using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class CalculoIndicesTests
{
    private static readonly Guid F1 = Guid.Parse("11111111-1111-7111-8111-111111111181");
    private static readonly Guid F9 = Guid.Parse("11111111-1111-7111-8111-111111111119");
    private static readonly DateOnly Dia = new(2026, 8, 27);

    [Fact]
    public void QuantidadeZero_HhPorUnidadeFicaNulo()
    {
        var frentes = new[] { Frente(F1, prevista: 26, concluida: 0) };
        var hh = new[] { Apt(F1, interna: false, minutos: 120) };
        var mapa = CalculoIndices.Calcular(frentes, hh, [], Dia);

        mapa[F1].HhPorUnidade.Should().BeNull();
        mapa[F1].HhDireto.Should().Be(2m);
        mapa[F1].AmostraInsuficiente.Should().BeTrue();
    }

    [Fact]
    public void SemIndireto_FatorZero_HhTotalIgualHhDireto()
    {
        var frentes = new[] { Frente(F1, prevista: 26, concluida: 2, hhOrc: 38) };
        var hh = new[] { Apt(F1, false, 180) };
        var prod = new[] { new ProducaoIndice(F1, Dia, 2) };
        var mapa = CalculoIndices.Calcular(frentes, hh, prod, Dia);

        mapa[F1].HhDireto.Should().Be(3m);
        mapa[F1].HhIndiretoRateado.Should().Be(0m);
        mapa[F1].HhTotal.Should().Be(3m);
        mapa[F1].HhPorUnidade.Should().Be(1.5m);
    }

    [Fact]
    public void ComIndireto_RateiaSobreODireto()
    {
        var frentes = new[]
        {
            Frente(F1, prevista: 10, concluida: 1),
            new FrenteIndice(F9, true, 0, 0, null, null)
        };
        var hh = new[]
        {
            Apt(F1, false, 60),
            Apt(F9, true, 60)
        };
        var prod = new[] { new ProducaoIndice(F1, Dia, 1) };
        var mapa = CalculoIndices.Calcular(frentes, hh, prod, Dia);

        mapa[F1].HhDireto.Should().Be(1m);
        mapa[F1].HhIndiretoRateado.Should().Be(1m);
        mapa[F1].HhTotal.Should().Be(2m);
        mapa[F1].HhPorUnidade.Should().Be(2m);
    }

    [Fact]
    public void AmostraMenorQueCinco_MarcaInsuficiente()
    {
        var frentes = new[] { Frente(F1, 26, 4) };
        var prod = Enumerable.Range(0, 4).Select(i => new ProducaoIndice(F1, Dia.AddDays(i), 1)).ToList();
        var mapa = CalculoIndices.Calcular(frentes, [], prod, Dia.AddDays(4));
        mapa[F1].AmostraInsuficiente.Should().BeTrue();
        mapa[F1].LancamentosProducao.Should().Be(4);
    }

    [Fact]
    public void RN19_AcoEstimadoUsaTaxaVezesConcluida()
    {
        var frentes = new[] { Frente(F1, 26, 10, taxaAco: 1180) };
        var mapa = CalculoIndices.Calcular(frentes, [], [], Dia);
        mapa[F1].AcoEstimadoKg.Should().Be(11800m);
    }

    [Fact]
    public void RN06b_ExcedePrevisaoEmMaisDeDezPorCento()
    {
        CalculoIndices.ExcedePrevisaoEmMaisDeDezPorCento(100, 110).Should().BeFalse();
        CalculoIndices.ExcedePrevisaoEmMaisDeDezPorCento(100, 111).Should().BeTrue();
        CalculoIndices.ExcedePrevisaoEmMaisDeDezPorCento(0, 10).Should().BeFalse();
    }

    [Fact]
    public void RN14_SemCustoHora_CustoFicaNulo()
    {
        var frentes = new[] { Frente(F1, 10, 1) };
        var hh = new[] { new ApontamentoIndice(F1, false, 60, null, Dia) };
        var mapa = CalculoIndices.Calcular(frentes, hh, [new ProducaoIndice(F1, Dia, 1)], Dia);
        mapa[F1].CustoDireto.Should().BeNull();
        mapa[F1].CustoTotal.Should().BeNull();
        mapa[F1].CustoPorUnidade.Should().BeNull();
    }

    [Fact]
    public void DesvioPercentual_ComparaRealComCotado()
    {
        var frentes = new[] { Frente(F1, 10, 1, hhOrc: 10) };
        var hh = new[] { Apt(F1, false, 660) }; // 11 HH
        var prod = new[] { new ProducaoIndice(F1, Dia, 1) };
        var mapa = CalculoIndices.Calcular(frentes, hh, prod, Dia);
        mapa[F1].DesvioPercentual.Should().Be(10m);
    }

    private static FrenteIndice Frente(
        Guid id,
        decimal prevista,
        decimal concluida,
        decimal? hhOrc = null,
        decimal? taxaAco = null) =>
        new(id, false, prevista, concluida, hhOrc, taxaAco);

    private static ApontamentoIndice Apt(Guid frente, bool interna, int minutos) =>
        new(frente, interna, minutos, CustoHora: 32.4m, Dia);
}
