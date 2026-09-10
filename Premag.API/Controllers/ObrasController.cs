using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ObrasController : ControllerBase
{
    private readonly IObraService _obraService;
    private readonly IProducaoService _producaoService;
    private readonly ILogger<ObrasController> _logger;

    public ObrasController(
        IObraService obraService,
        IProducaoService producaoService,
        ILogger<ObrasController> logger)
    {
        _obraService = obraService;
        _producaoService = producaoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] short? status,
        [FromQuery] bool incluirInternas = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _obraService.ListarAsync(status, incluirInternas, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "obras.listar");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var obra = await _obraService.ObterAsync(id, cancellationToken);
            if (obra is null)
                return NotFound(new { message = "Obra não encontrada" });
            return Ok(obra);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "obras.obter");
        }
    }

    [HttpGet("{id:guid}/frentes")]
    public async Task<IActionResult> GetFrentes(Guid id, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            var frentes = await _obraService.ListarFrentesAsync(id, null, null, cancellationToken);
            return Ok(await _producaoService.EnriquecerComIndicesAsync(frentes, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "obras.frentes");
        }
    }

    [HttpPost]
    [Authorize(Policy = "Diretoria")]
    public async Task<IActionResult> Post([FromBody] CriarObraDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var criada = await _obraService.CriarAsync(dto, cancellationToken);
            return StatusCode(201, criada);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "obras.criar");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Diretoria")]
    public async Task<IActionResult> Put(
        Guid id,
        [FromBody] AtualizarObraDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _obraService.AtualizarAsync(id, dto, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "obras.atualizar");
        }
    }
}
