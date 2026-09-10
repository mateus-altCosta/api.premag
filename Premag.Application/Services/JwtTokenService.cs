using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Premag.Application.Interfaces.Services;
using Premag.Core.Entities;

namespace Premag.Application.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DateTimeOffset ObterExpiraEm()
    {
        var minutes = _configuration.GetValue("Jwt:AccessTokenLifetimeMinutes", 30);
        if (minutes < 5) minutes = 5;
        if (minutes > 120) minutes = 120;
        return DateTimeOffset.UtcNow.AddMinutes(minutes);
    }

    public string CreateAccessToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = ObterExpiraEm();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NomeExibicao),
            new(ClaimTypes.Role, usuario.Perfil),
            new("tenant_id", usuario.TenantId.ToString()),
            new("perfil", usuario.Perfil)
        };

        if (!string.IsNullOrWhiteSpace(usuario.Email))
            claims.Add(new Claim(ClaimTypes.Email, usuario.Email));
        if (usuario.EquipeId is Guid equipeId)
            claims.Add(new Claim("equipe_id", equipeId.ToString()));
        if (usuario.ColaboradorId is Guid colaboradorId)
            claims.Add(new Claim("colaborador_id", colaboradorId.ToString()));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expira.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
