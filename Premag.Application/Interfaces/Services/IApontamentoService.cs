using Premag.Application.Common;
using Premag.Core.DTOs;

namespace Premag.Application.Interfaces.Services;

public interface IApontamentoService
{
    Task<TurnoDto> ObterTurnoAsync(Guid? equipeId, DateOnly? data, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task<ApontamentoDto> IniciarAsync(IniciarApontamentoDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default, bool origemLote = false);
    Task<EncerrarResultadoDto> EncerrarAsync(Guid id, EncerrarApontamentoDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default, bool origemLote = false);
    Task<ApontamentoDto> AjustarAsync(Guid id, AjustarApontamentoDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
    Task AnularAsync(Guid id, AnularApontamentoDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default);
}
