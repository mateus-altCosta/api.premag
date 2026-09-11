using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Premag.Core.Interfaces;

namespace Premag.Infrastructure.Storage;

/// <summary>Fotos no Storage do Supabase (bucket privado). A API continua servindo o JPEG autenticado.</summary>
public sealed class SupabaseArquivoStorage : IArquivoStorage
{
    private readonly HttpClient _http;
    private readonly string _bucket;

    public SupabaseArquivoStorage(IConfiguration configuration, IHttpClientFactory httpFactory)
    {
        var url = (configuration["Storage:Supabase:Url"] ?? "").Trim().TrimEnd('/');
        var key = configuration["Storage:Supabase:Key"] ?? "";
        _bucket = configuration["Storage:Supabase:Bucket"] ?? "premag-fotos";
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Storage:Supabase:Url e Storage:Supabase:Key são obrigatórios.");

        _http = httpFactory.CreateClient(nameof(SupabaseArquivoStorage));
        _http.BaseAddress = new Uri(url + "/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        _http.DefaultRequestHeaders.Remove("apikey");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("apikey", key);
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task GravarAsync(string chave, byte[] bytes, CancellationToken cancellationToken = default)
    {
        var path = Caminho(chave);
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        using var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        req.Headers.TryAddWithoutValidation("x-upsert", "true");
        using var res = await _http.SendAsync(req, cancellationToken);
        if (res.IsSuccessStatusCode)
            return;

        var corpo = await res.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Falha ao gravar foto no Storage ({(int)res.StatusCode}): {corpo}");
    }

    public async Task<byte[]?> LerAsync(string chave, CancellationToken cancellationToken = default)
    {
        using var res = await _http.GetAsync(Caminho(chave), cancellationToken);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private string Caminho(string chave)
    {
        var limpo = chave.Replace('\\', '/').TrimStart('/');
        return $"storage/v1/object/{_bucket}/{limpo}";
    }
}
