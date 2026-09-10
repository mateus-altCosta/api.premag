using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FrentesController : ControllerBase
{
    private readonly IObraService _obraService;
    private readonly IProducaoService _producaoService;
    private readonly ILogger<FrentesController> _logger;

    public FrentesController(
        IObraService obraService,
        IProducaoService producaoService,
        ILogger<FrentesController> logger)
    {
        _obraService = obraService;
        _producaoService = producaoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? obraId,
        [FromQuery] Guid? equipeId,
        [FromQuery] bool? ativa,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            var frentes = await _obraService.ListarFrentesAsync(obraId, equipeId, ativa, cancellationToken);
            return Ok(await _producaoService.EnriquecerComIndicesAsync(frentes, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "frentes.listar");
        }
    }

    [HttpPost]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> Post([FromBody] CriarFrenteDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var criada = await _obraService.CriarFrenteAsync(dto, cancellationToken);
            return StatusCode(201, criada);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "frentes.criar");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> Put(
        Guid id,
        [FromBody] CriarFrenteDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _obraService.AtualizarFrenteAsync(id, dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "frentes.atualizar");
        }
    }
}
