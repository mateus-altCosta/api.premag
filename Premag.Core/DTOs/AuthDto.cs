using System.ComponentModel.DataAnnotations;

namespace Premag.Core.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Usuário é obrigatório")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;

    public string? DispositivoId { get; set; }
}

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "Refresh token obrigatório")]
    public string RefreshToken { get; set; } = string.Empty;

    public string? DispositivoId { get; set; }
}

public class LogoutDto
{
    public string? RefreshToken { get; set; }
    public string? DispositivoId { get; set; }
}

public class AuthTokenPairDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiraEm { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public Guid? EquipeId { get; set; }
    public string Nome { get; set; } = string.Empty;
}

public class UsuarioAtualDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Perfil { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid? EquipeId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public IReadOnlyList<string> Permissoes { get; set; } = [];
}
