using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProducoesController : ControllerBase
{
    private readonly IProducaoService _producaoService;
    private readonly ILogger<ProducoesController> _logger;

    public ProducoesController(IProducaoService producaoService, ILogger<ProducoesController> logger)
    {
        _producaoService = producaoService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RegistrarProducaoDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            var criada = await _producaoService.RegistrarAsync(dto, quem, cancellationToken);
            return StatusCode(201, criada);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "producoes.registrar");
        }
    }
}
