namespace Premag.Core.Interfaces;

public interface IArquivoStorage
{
    Task GravarAsync(string chave, byte[] bytes, CancellationToken cancellationToken = default);
    Task<byte[]?> LerAsync(string chave, CancellationToken cancellationToken = default);
}
