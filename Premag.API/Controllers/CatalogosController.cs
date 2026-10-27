using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CatalogosController : ControllerBase
{
    private readonly ICatalogoService _catalogoService;
    private readonly ILogger<CatalogosController> _logger;

    public CatalogosController(ICatalogoService catalogoService, ILogger<CatalogosController> logger)
    {
        _catalogoService = catalogoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _catalogoService.ObterAsync(CadastroHttp.Quem(this), cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "catalogos");
        }
    }

    [HttpPut("configuracao")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> PutConfiguracao(
        [FromBody] ConfiguracaoDto dto,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _catalogoService.AtualizarJornadaAsync(dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "catalogos.jornada");
        }
    }

    [HttpPost("motivos")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> PostMotivo([FromBody] CriarMotivoParadaDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return StatusCode(201, await _catalogoService.CriarMotivoAsync(dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "catalogos.motivos.criar");
        }
    }

    [HttpPut("motivos/{id:guid}")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> PutMotivo(
        Guid id,
        [FromBody] CriarMotivoParadaDto dto,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _catalogoService.AtualizarMotivoAsync(id, dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "catalogos.motivos.atualizar");
        }
    }

    [HttpDelete("motivos/{id:guid}")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> DeleteMotivo(Guid id, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            await _catalogoService.InativarMotivoAsync(id, quem, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "catalogos.motivos.inativar");
        }
    }
}
