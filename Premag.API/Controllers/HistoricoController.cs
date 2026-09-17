using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/historico")]
[Authorize]
public class HistoricoController : ControllerBase
{
    private readonly IHistoricoService _historico;
    private readonly ILogger<HistoricoController> _logger;

    public HistoricoController(IHistoricoService historico, ILogger<HistoricoController> logger)
    {
        _historico = historico;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int take = 80, CancellationToken cancellationToken = default)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _historico.ListarAsync(quem, take, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "historico");
        }
    }
}
