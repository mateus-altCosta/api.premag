using FluentAssertions;
using Premag.Core;
using Premag.Core.Constants;

namespace Premag.UnitTests;

public class PermissoesTests
{
    [Fact]
    public void Diretoria_HerdaGerenteEEncarregado()
    {
        Permissoes.Efetivas(NomesPerfil.Diretoria).Should().Equal(
            Permissoes.Encarregado, Permissoes.Gerente, Permissoes.Diretoria);
        Permissoes.Tem(NomesPerfil.Diretoria, Permissoes.Gerente).Should().BeTrue();
    }

    [Fact]
    public void Encarregado_NaoTemGerente()
    {
        Permissoes.Tem(NomesPerfil.Encarregado, Permissoes.Gerente).Should().BeFalse();
    }

    [Fact]
    public void Gerente_RedefineSoEncarregado()
    {
        Permissoes.PodeRedefinirSenha(NomesPerfil.Gerente, NomesPerfil.Encarregado).Should().BeTrue();
        Permissoes.PodeRedefinirSenha(NomesPerfil.Gerente, NomesPerfil.Gerente).Should().BeFalse();
        Permissoes.PodeRedefinirSenha(NomesPerfil.Encarregado, NomesPerfil.Encarregado).Should().BeFalse();
        Permissoes.PodeRedefinirSenha(NomesPerfil.Admin, NomesPerfil.Diretoria).Should().BeTrue();
    }
}
