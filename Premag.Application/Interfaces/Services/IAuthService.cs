using Premag.Application.Common;
using Premag.Core.DTOs;

namespace Premag.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthTokenPairDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<AuthTokenPairDto?> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutDto dto, CancellationToken cancellationToken = default);
    Task<UsuarioAtualDto?> GetMeAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task AlterarSenhaAsync(Guid usuarioId, AlterarSenhaDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioListaDto>> ListarUsuariosAsync(UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task RedefinirSenhaAsync(Guid id, RedefinirSenhaDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
}
