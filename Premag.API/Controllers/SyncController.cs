using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISincronizacaoService _sincronizacao;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISincronizacaoService sincronizacao, ILogger<SyncController> logger)
    {
        _sincronizacao = sincronizacao;
        _logger = logger;
    }

    [HttpPost("lote")]
    public async Task<IActionResult> Lote([FromBody] EnviarLoteDto dto, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });

        try
        {
            return Ok(await _sincronizacao.ProcessarLoteAsync(dto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "sync.lote");
        }
    }
}
