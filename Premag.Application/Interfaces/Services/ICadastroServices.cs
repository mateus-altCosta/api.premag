using Premag.Application.Common;
using Premag.Core.DTOs;

namespace Premag.Application.Interfaces.Services;

public interface ICatalogoService
{
    Task<CatalogoDto> ObterAsync(CancellationToken cancellationToken = default);
}

public interface IEquipeService
{
    Task<IReadOnlyList<EquipeDto>> ListarAsync(UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ColaboradorDto>> ListarColaboradoresAsync(Guid equipeId, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<ColaboradorDto> CriarColaboradorAsync(CriarColaboradorDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<ColaboradorDto> TransferirAsync(Guid colaboradorId, TransferirColaboradorDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task ExcluirColaboradorAsync(Guid colaboradorId, UsuarioLogado quem, CancellationToken cancellationToken = default);
}

public interface IObraService
{
    Task<IReadOnlyList<ObraDto>> ListarAsync(short? status, bool incluirInternas, CancellationToken cancellationToken = default);
    Task<ObraDto?> ObterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ObraDto> CriarAsync(CriarObraDto dto, CancellationToken cancellationToken = default);
    Task<ObraDto> AtualizarAsync(Guid id, AtualizarObraDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FrenteDto>> ListarFrentesAsync(Guid? obraId, Guid? equipeId, bool? ativa, CancellationToken cancellationToken = default);
    Task<FrenteDto> CriarFrenteAsync(CriarFrenteDto dto, CancellationToken cancellationToken = default);
    Task<FrenteDto> AtualizarFrenteAsync(Guid id, CriarFrenteDto dto, CancellationToken cancellationToken = default);
}
