using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiarioController : ControllerBase
{
    private readonly IDiarioService _diario;
    private readonly ILogger<DiarioController> _logger;

    public DiarioController(IDiarioService diario, ILogger<DiarioController> logger)
    {
        _diario = diario;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? data,
        [FromQuery] Guid? equipeId,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _diario.ObterAsync(data, equipeId, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "diario");
        }
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf(
        [FromQuery] DateOnly? data,
        [FromQuery] Guid? equipeId,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            var bytes = await _diario.GerarPdfAsync(data, equipeId, quem, cancellationToken);
            var dia = data ?? DateOnly.FromDateTime(DateTime.Today);
            return File(bytes, "application/pdf", $"diario-{dia:yyyy-MM-dd}.pdf");
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "diario.pdf");
        }
    }
}
