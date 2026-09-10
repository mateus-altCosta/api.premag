namespace Premag.Core.Entities;

/// <summary>
/// Usuário de acesso. Mesmo padrão da Confraria: POCO + SenhaHash (BCrypt), sem ASP.NET Identity.
/// </summary>
public class Usuario
{
    public Guid Id { get; set; } = GeradorId.Novo();
    public Guid TenantId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string NomeExibicao { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public Guid? ColaboradorId { get; set; }
    public Guid? EquipeId { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTimeOffset? UltimoAcesso { get; set; }
    public DateTimeOffset DataCriacao { get; set; }

    public Colaborador? Colaborador { get; set; }
    public Equipe? Equipe { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
