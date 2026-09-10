namespace Premag.Core.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = GeradorId.Novo();
    public Guid TenantId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? DispositivoId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
