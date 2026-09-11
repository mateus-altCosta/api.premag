using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/fechamentos")]
[Authorize]
public class FechamentosController : ControllerBase
{
    private readonly IFechamentoService _fechamentos;
    private readonly ILogger<FechamentosController> _logger;

    public FechamentosController(IFechamentoService fechamentos, ILogger<FechamentosController> logger)
    {
        _fechamentos = fechamentos;
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
            return Ok(await _fechamentos.ObterAsync(data, equipeId, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "fechamentos");
        }
    }

    [HttpPost("fechar")]
    public async Task<IActionResult> Fechar([FromBody] FecharDiaDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _fechamentos.FecharAsync(dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "fechamentos.fechar");
        }
    }

    [HttpPost("reabrir")]
    public async Task<IActionResult> Reabrir([FromBody] ReabrirDiaDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _fechamentos.ReabrirAsync(dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "fechamentos.reabrir");
        }
    }
}
