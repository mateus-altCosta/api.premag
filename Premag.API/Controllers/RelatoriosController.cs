using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Gerente")]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _relatorios;
    private readonly ILogger<RelatoriosController> _logger;

    public RelatoriosController(IRelatorioService relatorios, ILogger<RelatoriosController> logger)
    {
        _relatorios = relatorios;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string tipo = "produtividade",
        [FromQuery] string periodo = "hoje",
        [FromQuery] Guid? obraId = null,
        CancellationToken cancellationToken = default)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _relatorios.ObterAsync(tipo, periodo, obraId, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "relatorios");
        }
    }

    [HttpGet("csv")]
    public async Task<IActionResult> Csv(
        [FromQuery] string tipo = "produtividade",
        [FromQuery] string periodo = "hoje",
        [FromQuery] Guid? obraId = null,
        CancellationToken cancellationToken = default)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            var bytes = await _relatorios.GerarCsvAsync(tipo, periodo, obraId, quem, cancellationToken);
            return File(bytes, "text/csv; charset=utf-8", $"relatorio-{tipo}-{periodo}.csv");
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "relatorios.csv");
        }
    }
}
