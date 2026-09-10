using Microsoft.Extensions.Configuration;
using Premag.Core.Interfaces;

namespace Premag.Infrastructure.Storage;

public sealed class LocalArquivoStorage : IArquivoStorage
{
    private readonly string _raiz;

    public LocalArquivoStorage(IConfiguration configuration)
    {
        var cfg = configuration["Storage:Root"];
        _raiz = string.IsNullOrWhiteSpace(cfg)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "fotos")
            : cfg;
        Directory.CreateDirectory(_raiz);
    }

    public async Task GravarAsync(string chave, byte[] bytes, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_raiz, chave.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
    }

    public async Task<byte[]?> LerAsync(string chave, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_raiz, chave.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return null;
        return await File.ReadAllBytesAsync(path, cancellationToken);
    }
}
