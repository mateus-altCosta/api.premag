using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ColaboradoresController : ControllerBase
{
    private readonly IEquipeService _equipeService;
    private readonly ILogger<ColaboradoresController> _logger;

    public ColaboradoresController(IEquipeService equipeService, ILogger<ColaboradoresController> logger)
    {
        _equipeService = equipeService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> Post([FromBody] CriarColaboradorDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            var criado = await _equipeService.CriarColaboradorAsync(dto, quem, cancellationToken);
            return StatusCode(201, criado);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "colaboradores.criar");
        }
    }

    [HttpPut("{id:guid}/equipe")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> Transferir(
        Guid id,
        [FromBody] TransferirColaboradorDto dto,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _equipeService.TransferirAsync(id, dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "colaboradores.transferir");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            await _equipeService.ExcluirColaboradorAsync(id, quem, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "colaboradores.excluir");
        }
    }
}
