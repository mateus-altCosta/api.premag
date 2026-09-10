using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Gerente")]
public class ImportacoesController : ControllerBase
{
    private readonly IImportacaoService _importacao;
    private readonly ILogger<ImportacoesController> _logger;

    public ImportacoesController(IImportacaoService importacao, ILogger<ImportacoesController> logger)
    {
        _importacao = importacao;
        _logger = logger;
    }

    [HttpPost("afd")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Afd(IFormFile? arquivo, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            var texto = await LerAsync(arquivo, cancellationToken);
            return Ok(await _importacao.ImportarAfdAsync(texto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "importacoes.afd");
        }
    }

    [HttpPost("colaboradores")]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Colaboradores(IFormFile? arquivo, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            var texto = await LerAsync(arquivo, cancellationToken);
            return Ok(await _importacao.ImportarColaboradoresAsync(texto, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "importacoes.colaboradores");
        }
    }

    private static async Task<string> LerAsync(IFormFile? arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
            throw new Premag.Core.Exceptions.RegraNegocioException("ARQUIVO_OBRIGATORIO", "Envie o arquivo.", 422);
        await using var s = arquivo.OpenReadStream();
        using var r = new StreamReader(s);
        return await r.ReadToEndAsync(cancellationToken);
    }
}
