using FluentAssertions;
using Premag.Core;
using Premag.Core.Constants;

namespace Premag.UnitTests;

public class CadastroRegrasTests
{
    [Fact]
    public void RN13_EncarregadoNaoVeCusto()
    {
        Permissoes.Tem(NomesPerfil.Encarregado, Permissoes.Gerente).Should().BeFalse();
    }

    [Fact]
    public void RN10_GerenteVeTodasAsEquipes()
    {
        Permissoes.Tem(NomesPerfil.Gerente, Permissoes.Gerente).Should().BeTrue();
        Permissoes.Tem(NomesPerfil.Admin, Permissoes.Gerente).Should().BeTrue();
    }
}
