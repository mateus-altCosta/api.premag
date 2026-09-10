using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TurnoController : ControllerBase
{
    private readonly IApontamentoService _apontamentoService;
    private readonly ILogger<TurnoController> _logger;

    public TurnoController(IApontamentoService apontamentoService, ILogger<TurnoController> logger)
    {
        _apontamentoService = apontamentoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? equipeId,
        [FromQuery] DateOnly? data,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _apontamentoService.ObterTurnoAsync(equipeId, data, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "turno");
        }
    }
}
