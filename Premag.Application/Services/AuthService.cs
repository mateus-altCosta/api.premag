using Microsoft.EntityFrameworkCore;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(ApplicationDbContext context, IRefreshTokenService refreshTokenService)
    {
        _context = context;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthTokenPairDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var login = dto.Usuario.Trim();
        var usuario = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.Ativo && (u.UserName == login || (u.Email != null && u.Email.ToLower() == login.ToLower())),
                cancellationToken);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash))
            throw new UnauthorizedAccessException("Credenciais inválidas");

        usuario.UltimoAcesso = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await _refreshTokenService.IssueForUserAsync(usuario, dto.DispositivoId, cancellationToken);
    }

    public Task<AuthTokenPairDto?> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default) =>
        _refreshTokenService.RotateAsync(dto.RefreshToken, dto.DispositivoId, cancellationToken);

    public Task LogoutAsync(LogoutDto dto, CancellationToken cancellationToken = default) =>
        _refreshTokenService.RevokeAsync(dto.RefreshToken, null, dto.DispositivoId, cancellationToken);

    public async Task<UsuarioAtualDto?> GetMeAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Ativo, cancellationToken);

        if (usuario == null)
            return null;

        return new UsuarioAtualDto
        {
            Id = usuario.Id,
            UserName = usuario.UserName,
            Nome = usuario.NomeExibicao,
            Email = usuario.Email,
            Perfil = usuario.Perfil,
            TenantId = usuario.TenantId,
            EquipeId = usuario.EquipeId,
            ColaboradorId = usuario.ColaboradorId,
            Permissoes = Permissoes.Efetivas(usuario.Perfil)
        };
    }
}
