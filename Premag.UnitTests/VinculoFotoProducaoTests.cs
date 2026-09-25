using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class VinculoFotoProducaoTests
{
    private static readonly Guid P1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid Apt = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly DateTimeOffset T0 = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SemLancamentoNoDia_FotoLancaQuantidade()
    {
        var d = VinculoFotoProducao.Decidir([], null, 2);
        d.LancarNova.Should().BeTrue();
        d.ProducaoIdParaVincular.Should().BeNull();
    }

    [Fact]
    public void EncerrarDuasPecasDepoisFotoDuas_NaoSomaDeNovo()
    {
        var doDia = new[]
        {
            new VinculoFotoProducao.LancamentoDia(P1, Apt, null, 2, T0)
        };

        var d = VinculoFotoProducao.Decidir(doDia, Apt, 2);
        d.LancarNova.Should().BeFalse();
        d.ProducaoIdParaVincular.Should().Be(P1);
    }

    [Fact]
    public void EncerrarSemApontamentoNaFoto_MesmaQuantidade_NaoSomaDeNovo()
    {
        var doDia = new[]
        {
            new VinculoFotoProducao.LancamentoDia(P1, Apt, null, 2, T0)
        };

        var d = VinculoFotoProducao.Decidir(doDia, null, 2);
        d.LancarNova.Should().BeFalse();
        d.ProducaoIdParaVincular.Should().Be(P1);
    }

    [Fact]
    public void SegundaFotoNoMesmoDia_NaoSomaDeNovo()
    {
        var doDia = new[]
        {
            new VinculoFotoProducao.LancamentoDia(P1, Apt, Guid.NewGuid(), 2, T0)
        };

        var d = VinculoFotoProducao.Decidir(doDia, null, 2);
        d.LancarNova.Should().BeFalse();
        d.ProducaoIdParaVincular.Should().BeNull();
    }
}
