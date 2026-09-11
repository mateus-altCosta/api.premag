using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class CalculoFechamentoTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 11);
    private static readonly Guid Equipe = Guid.Parse("11111111-1111-7111-8111-111111111301");

    [Fact]
    public void RN15_DiaAnteriorAoPrazo_FechaPorCalendario()
    {
        CalculoFechamento.FechadoPorCalendario(new DateOnly(2026, 9, 7), Hoje, 3).Should().BeTrue();
        CalculoFechamento.FechadoPorCalendario(new DateOnly(2026, 9, 8), Hoje, 3).Should().BeFalse();
        CalculoFechamento.FechadoPorCalendario(Hoje, Hoje, 3).Should().BeFalse();
    }

    [Fact]
    public void RegistroDaEquipe_FechaSoAquelaEquipe()
    {
        var regs = new[] { new RegistroFechamento(Hoje, Equipe, null) };
        CalculoFechamento.FechadoPorRegistro(Hoje, Equipe, regs).Should().BeTrue();
        CalculoFechamento.FechadoPorRegistro(Hoje, Guid.NewGuid(), regs).Should().BeFalse();
    }

    [Fact]
    public void RegistroDaPlanta_FechaTodasAsEquipes()
    {
        var regs = new[] { new RegistroFechamento(Hoje, null, null) };
        CalculoFechamento.FechadoPorRegistro(Hoje, Equipe, regs).Should().BeTrue();
    }

    [Fact]
    public void Reabertura_LiberaODia()
    {
        var regs = new[] { new RegistroFechamento(Hoje, Equipe, DateTimeOffset.UtcNow) };
        CalculoFechamento.FechadoPorRegistro(Hoje, Equipe, regs).Should().BeFalse();
    }
}
