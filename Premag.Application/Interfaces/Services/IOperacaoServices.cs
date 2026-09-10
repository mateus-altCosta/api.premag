using Premag.Application.Common;
using Premag.Core.DTOs;
using Premag.Core.Enums;

namespace Premag.Application.Interfaces.Services;

public interface IFotoService
{
    Task<IReadOnlyList<FotoDto>> ListarAsync(DateOnly? data, Guid? frenteId, Guid? colaboradorId, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<FotoDto> RegistrarAsync(Guid clienteUuid, Guid frenteId, Guid? colaboradorId, Guid? apontamentoId, TipoFoto tipo, decimal? quantidade, string? observacao, byte[] jpeg, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<(byte[] Bytes, string ContentType)?> ObterArquivoAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IDiarioService
{
    Task<DiarioDto> ObterAsync(DateOnly? data, Guid? equipeId, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<byte[]> GerarPdfAsync(DateOnly? data, Guid? equipeId, UsuarioLogado quem, CancellationToken cancellationToken = default);
}

public interface IOcorrenciaService
{
    Task<IReadOnlyList<OcorrenciaDto>> ListarAsync(DateOnly? data, bool pendentes, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task ReconhecerAsync(Guid id, ReconhecerOcorrenciaDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task DetectarAgoraAsync(CancellationToken cancellationToken = default);
}

public interface IRelatorioService
{
    Task<RelatorioDto> ObterAsync(string tipo, string periodo, Guid? obraId, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<byte[]> GerarCsvAsync(string tipo, string periodo, Guid? obraId, UsuarioLogado quem, CancellationToken cancellationToken = default);
}

public interface IImportacaoService
{
    Task<ImportacaoResultadoDto> ImportarAfdAsync(string conteudo, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<ImportacaoResultadoDto> ImportarColaboradoresAsync(string csv, UsuarioLogado quem, CancellationToken cancellationToken = default);
}
