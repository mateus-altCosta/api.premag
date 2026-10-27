using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Exceptions;
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

    public async Task AlterarSenhaAsync(Guid usuarioId, AlterarSenhaDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Ativo, cancellationToken)
            ?? throw new UnauthorizedAccessException("Usuário não autenticado");

        if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.SenhaHash))
            throw new RegraNegocioException("SENHA_ATUAL", "A senha atual não confere.", 422);

        var nova = CadastroTexto.SenhaNova(dto.SenhaNova);
        if (BCrypt.Net.BCrypt.Verify(nova, usuario.SenhaHash))
            throw new RegraNegocioException("SENHA_IGUAL", "A senha nova deve ser diferente da atual.", 422);

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(nova);
        _context.AuditLogs.Add(new Premag.Core.Entities.AuditLog
        {
            TenantId = usuario.TenantId,
            Entidade = "Usuario",
            EntidadeId = usuario.Id,
            Acao = "alterar-senha",
            UsuarioId = usuario.Id,
            Em = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UsuarioListaDto>> ListarUsuariosAsync(
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima lista usuários.", 403);

        return await _context.Users.AsNoTracking()
            .Where(u => u.Ativo)
            .OrderBy(u => u.NomeExibicao)
            .Select(u => new UsuarioListaDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Nome = u.NomeExibicao,
                Perfil = u.Perfil,
                EquipeId = u.EquipeId,
                EquipeNome = u.Equipe != null ? u.Equipe.Nome : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task RedefinirSenhaAsync(
        Guid id,
        RedefinirSenhaDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima redefine senha.", 403);

        if (id == quem.Id)
            throw new RegraNegocioException("SENHA_PROPRIA", "Troque a própria senha em Conta.", 422);

        var alvo = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Ativo, cancellationToken)
            ?? throw new RegraNegocioException("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.", 404);

        if (!Permissoes.PodeRedefinirSenha(quem.Perfil, alvo.Perfil))
            throw new RegraNegocioException("SEM_PERMISSAO", "Não redefine senha de perfil igual ou acima.", 403);

        var nova = CadastroTexto.SenhaNova(dto.SenhaNova);
        alvo.SenhaHash = BCrypt.Net.BCrypt.HashPassword(nova);
        _context.AuditLogs.Add(new Premag.Core.Entities.AuditLog
        {
            TenantId = alvo.TenantId,
            Entidade = "Usuario",
            EntidadeId = alvo.Id,
            Acao = "redefinir-senha",
            Depois = alvo.UserName,
            UsuarioId = quem.Id,
            Em = DateTimeOffset.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _refreshTokenService.RevokeAsync(null, alvo.Id, null, cancellationToken);
    }
}
