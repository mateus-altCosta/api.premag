using Premag.Application.Common;
using Premag.Core.DTOs;

namespace Premag.Application.Interfaces.Services;

public interface IProducaoService
{
    Task<ProducaoResultadoDto> RegistrarAsync(RegistrarProducaoDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FrenteDto>> EnriquecerComIndicesAsync(
        IReadOnlyList<FrenteDto> frentes,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default);
}
