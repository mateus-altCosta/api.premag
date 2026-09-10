using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Premag.Application.Interfaces.Services;
using Premag.Core.Enums;

namespace Premag.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FotosController : ControllerBase
{
    private readonly IFotoService _fotos;
    private readonly ILogger<FotosController> _logger;

    public FotosController(IFotoService fotos, ILogger<FotosController> logger)
    {
        _fotos = fotos;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? data,
        [FromQuery] Guid? frenteId,
        [FromQuery] Guid? colaboradorId,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            return Ok(await _fotos.ListarAsync(data, frenteId, colaboradorId, quem, cancellationToken));
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "fotos.listar");
        }
    }

    [HttpPost]
    [RequestSizeLimit(400_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 400_000)]
    public async Task<IActionResult> Post(
        [FromForm] Guid clienteUuid,
        [FromForm] Guid frenteId,
        [FromForm] Guid? colaboradorId,
        [FromForm] Guid? apontamentoId,
        [FromForm] string tipo,
        [FromForm] decimal? quantidade,
        [FromForm] string? observacao,
        IFormFile? arquivo,
        CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        if (arquivo is null || arquivo.Length == 0)
            return CadastroHttp.Falha(
                new Premag.Core.Exceptions.RegraNegocioException("ARQUIVO_OBRIGATORIO", "Envie o arquivo JPEG.", 422),
                _logger, "fotos.registrar");

        if (!Enum.TryParse<TipoFoto>(tipo, ignoreCase: true, out var tipoFoto))
            tipoFoto = TipoFoto.Avanco;

        try
        {
            await using var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms, cancellationToken);
            var dto = await _fotos.RegistrarAsync(
                clienteUuid, frenteId, colaboradorId, apontamentoId, tipoFoto, quantidade, observacao,
                ms.ToArray(), quem, cancellationToken);
            return StatusCode(201, dto);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "fotos.registrar");
        }
    }

    [HttpGet("{id:guid}/arquivo")]
    public Task<IActionResult> Arquivo(Guid id, CancellationToken cancellationToken) =>
        Servir(id, cancellationToken);

    [HttpGet("{id:guid}/thumb")]
    public Task<IActionResult> Thumb(Guid id, CancellationToken cancellationToken) =>
        Servir(id, cancellationToken);

    private async Task<IActionResult> Servir(Guid id, CancellationToken cancellationToken)
    {
        var quem = CadastroHttp.Quem(this);
        if (quem is null)
            return Unauthorized(new { message = "Usuário não autenticado" });
        try
        {
            var arq = await _fotos.ObterArquivoAsync(id, cancellationToken);
            if (arq is null)
                return NotFound();
            return File(arq.Value.Bytes, arq.Value.ContentType);
        }
        catch (Exception ex)
        {
            return CadastroHttp.Falha(ex, _logger, "fotos.arquivo");
        }
    }
}
