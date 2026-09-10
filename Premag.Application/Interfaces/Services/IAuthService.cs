using Premag.Core.DTOs;

namespace Premag.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthTokenPairDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<AuthTokenPairDto?> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutDto dto, CancellationToken cancellationToken = default);
    Task<UsuarioAtualDto?> GetMeAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
