using Premag.Core.Constants;

namespace Premag.Core;

/// <summary>
/// Perfis cumulativos (seção 5): Diretoria herda Gerente, que herda Encarregado.
/// </summary>
public static class Permissoes
{
    public const string Encarregado = NomesPerfil.Encarregado;
    public const string Gerente = NomesPerfil.Gerente;
    public const string Diretoria = NomesPerfil.Diretoria;
    public const string Admin = NomesPerfil.Admin;

    public static IReadOnlyList<string> Efetivas(string perfil) => perfil switch
    {
        NomesPerfil.Admin => [Encarregado, Gerente, Diretoria, Admin],
        NomesPerfil.Diretoria => [Encarregado, Gerente, Diretoria],
        NomesPerfil.Gerente => [Encarregado, Gerente],
        NomesPerfil.Encarregado => [Encarregado],
        _ => []
    };

    public static bool Tem(string perfil, string exigido) =>
        Efetivas(perfil).Contains(exigido);
}
