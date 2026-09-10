using System.Security.Claims;

namespace Premag.Application.Common;

public sealed record UsuarioLogado(Guid Id, string Perfil, Guid? EquipeId, string? Ip);

public static class UsuarioLogadoFactory
{
    public static UsuarioLogado? From(ClaimsPrincipal user, string? ip)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(raw, out var id))
            return null;

        var perfil = user.FindFirst(ClaimTypes.Role)?.Value
            ?? user.FindFirst("perfil")?.Value
            ?? string.Empty;
        Guid? equipe = Guid.TryParse(user.FindFirst("equipe_id")?.Value, out var e) ? e : null;
        return new UsuarioLogado(id, perfil, equipe, ip);
    }
}
