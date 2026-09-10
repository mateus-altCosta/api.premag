using Premag.Application.Common;
using Premag.Core.DTOs;

namespace Premag.Application.Interfaces.Services;

public interface ISincronizacaoService
{
    Task<LoteResultadoDto> ProcessarLoteAsync(EnviarLoteDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
}
