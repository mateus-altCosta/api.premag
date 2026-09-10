using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OcorrenciasController : ControllerBase
{
    private readonly IOcorrenciaService _ocorrencias;
    private readonly ILogger<OcorrenciasController> _logger;

    public OcorrenciasController(IOcorrenciaService ocorrencias, ILogger<OcorrenciasController> logger)
    {
        _ocorrencias = ocorrencias;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? data,
        [FromQuery] bool pendentes = true,
        CancellationToken cancellationToken = default)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _ocorrencias.ListarAsync(data, pendentes, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "ocorrencias");
        }
    }

    [HttpPost("{id:guid}/reconhecer")]
    public async Task<IActionResult> Reconhecer(
        Guid id,
        [FromBody] ReconhecerOcorrenciaDto dto,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            await _ocorrencias.ReconhecerAsync(id, dto, quem, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "ocorrencias.reconhecer");
        }
    }
}
