using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public RefreshTokenService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<AuthTokenPairDto> IssueForUserAsync(
        Usuario usuario,
        string? dispositivoId,
        CancellationToken cancellationToken = default)
    {
        var dispositivo = NormalizarDispositivo(dispositivoId);
        await RevogarDoDispositivoAsync(usuario.Id, dispositivo, cancellationToken);

        var plain = GenerateSecureToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            TenantId = usuario.TenantId,
            UsuarioId = usuario.Id,
            Token = HashToken(plain),
            DispositivoId = dispositivo,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(HorasRefresh()),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        return MontarPar(usuario, plain);
    }

    public async Task<AuthTokenPairDto?> RotateAsync(
        string refreshTokenPlain,
        string? dispositivoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenPlain))
            return null;

        var hash = HashToken(refreshTokenPlain.Trim());
        var now = DateTimeOffset.UtcNow;

        var existing = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(
                r => r.Token == hash && r.RevokedAt == null && r.ExpiresAt > now,
                cancellationToken);

        if (existing?.Usuario is not { Ativo: true })
            return null;

        existing.RevokedAt = now;

        var dispositivo = NormalizarDispositivo(dispositivoId ?? existing.DispositivoId);
        var plain = GenerateSecureToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            TenantId = existing.Usuario.TenantId,
            UsuarioId = existing.Usuario.Id,
            Token = HashToken(plain),
            DispositivoId = dispositivo,
            ExpiresAt = now.AddHours(HorasRefresh()),
            CreatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);

        return MontarPar(existing.Usuario, plain);
    }

    public async Task RevokeAsync(
        string? refreshTokenPlain,
        Guid? usuarioId,
        string? dispositivoId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _context.RefreshTokens.IgnoreQueryFilters().Where(r => r.RevokedAt == null);

        if (!string.IsNullOrWhiteSpace(refreshTokenPlain))
        {
            var hash = HashToken(refreshTokenPlain.Trim());
            query = query.Where(r => r.Token == hash);
        }
        else if (usuarioId is Guid uid)
        {
            query = query.Where(r => r.UsuarioId == uid);
            if (!string.IsNullOrWhiteSpace(dispositivoId))
                query = query.Where(r => r.DispositivoId == dispositivoId);
        }
        else
        {
            return;
        }

        var tokens = await query.ToListAsync(cancellationToken);
        foreach (var t in tokens)
            t.RevokedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RevogarDoDispositivoAsync(Guid usuarioId, string dispositivoId, CancellationToken cancellationToken)
    {
        var abertos = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(r => r.UsuarioId == usuarioId && r.DispositivoId == dispositivoId && r.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var t in abertos)
            t.RevokedAt = DateTimeOffset.UtcNow;
    }

    private AuthTokenPairDto MontarPar(Usuario usuario, string refreshPlain) => new()
    {
        AccessToken = _jwtTokenService.CreateAccessToken(usuario),
        RefreshToken = refreshPlain,
        ExpiraEm = _jwtTokenService.ObterExpiraEm(),
        Perfil = usuario.Perfil,
        EquipeId = usuario.EquipeId,
        Nome = usuario.NomeExibicao
    };

    private int HorasRefresh()
    {
        var hours = _configuration.GetValue("Jwt:RefreshTokenLifetimeHours", 720);
        if (hours < 1) hours = 1;
        if (hours > 720) hours = 720;
        return hours;
    }

    private static string NormalizarDispositivo(string? dispositivoId) =>
        string.IsNullOrWhiteSpace(dispositivoId) ? "desconhecido" : dispositivoId.Trim();

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }
}
