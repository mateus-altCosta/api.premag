using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApontamentosController : ControllerBase
{
    private readonly IApontamentoService _apontamentoService;
    private readonly ILogger<ApontamentosController> _logger;

    public ApontamentosController(IApontamentoService apontamentoService, ILogger<ApontamentosController> logger)
    {
        _apontamentoService = apontamentoService;
        _logger = logger;
    }

    [HttpPost("iniciar")]
    public async Task<IActionResult> Iniciar([FromBody] IniciarApontamentoDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            var criado = await _apontamentoService.IniciarAsync(dto, quem, cancellationToken);
            return Ok(criado);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "apontamentos.iniciar");
        }
    }

    [HttpPost("{id:guid}/encerrar")]
    public async Task<IActionResult> Encerrar(
        Guid id,
        [FromBody] EncerrarApontamentoDto dto,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _apontamentoService.EncerrarAsync(id, dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "apontamentos.encerrar");
        }
    }
}
