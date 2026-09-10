using Premag.Core.Entities;

namespace Premag.Application.Interfaces.Services;

public interface IJwtTokenService
{
    string CreateAccessToken(Usuario usuario);
    DateTimeOffset ObterExpiraEm();
}
