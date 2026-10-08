using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EquipesController : ControllerBase
{
    private readonly IEquipeService _equipeService;
    private readonly ILogger<EquipesController> _logger;

    public EquipesController(IEquipeService equipeService, ILogger<EquipesController> logger)
    {
        _equipeService = equipeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _equipeService.ListarAsync(quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "equipes.listar");
        }
    }

    [HttpPost]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> Post([FromBody] CriarEquipeDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return StatusCode(201, await _equipeService.CriarAsync(dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "equipes.criar");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Gerente")]
    public async Task<IActionResult> Put(Guid id, [FromBody] CriarEquipeDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _equipeService.AtualizarAsync(id, dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "equipes.atualizar");
        }
    }

    [HttpGet("{id:guid}/colaboradores")]
    public async Task<IActionResult> GetColaboradores(Guid id, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _equipeService.ListarColaboradoresAsync(id, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "equipes.colaboradores");
        }
    }
}
