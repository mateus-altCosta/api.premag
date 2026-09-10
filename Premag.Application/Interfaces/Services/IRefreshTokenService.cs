using Premag.Core.DTOs;
using Premag.Core.Entities;

namespace Premag.Application.Interfaces.Services;

public interface IRefreshTokenService
{
    Task<AuthTokenPairDto> IssueForUserAsync(Usuario usuario, string? dispositivoId, CancellationToken cancellationToken = default);
    Task<AuthTokenPairDto?> RotateAsync(string refreshTokenPlain, string? dispositivoId, CancellationToken cancellationToken = default);
    Task RevokeAsync(string? refreshTokenPlain, Guid? usuarioId, string? dispositivoId, CancellationToken cancellationToken = default);
}
