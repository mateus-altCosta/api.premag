using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/push")]
[Authorize]
public class PushController : ControllerBase
{
    private readonly IPushService _push;
    private readonly ILogger<PushController> _logger;

    public PushController(IPushService push, ILogger<PushController> logger)
    {
        _push = push;
        _logger = logger;
    }

    [HttpGet("chave")]
    public IActionResult Chave()
    {
        var pub = _push.ChavePublica;
        if (string.IsNullOrWhiteSpace(pub))
            return StatusCode(503, new { message = "Push não configurado." });
        return Ok(new PushChaveDto { ChavePublica = pub });
    }

    [HttpPost("inscrever")]
    public async Task<IActionResult> Inscrever([FromBody] InscreverPushDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            await _push.InscreverAsync(dto, quem, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "push.inscrever");
        }
    }

    [HttpPost("desinscrever")]
    public async Task<IActionResult> Desinscrever([FromBody] InscreverPushDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            await _push.DesinscreverAsync(dto.Endpoint, quem, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "push.desinscrever");
        }
    }
}
