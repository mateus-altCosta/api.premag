using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Gerente")]
public class UsuariosController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(IAuthService auth, ILogger<UsuariosController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _auth.ListarUsuariosAsync(quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "usuarios.listar");
        }
    }

    [HttpPut("{id:guid}/senha")]
    public async Task<IActionResult> Senha(Guid id, [FromBody] RedefinirSenhaDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            await _auth.RedefinirSenhaAsync(id, dto, quem, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "usuarios.senha");
        }
    }
}
