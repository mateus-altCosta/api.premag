using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Core.Exceptions;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;
using WebPush;

namespace Premag.Application.Services;

public class PushService : IPushService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;
    private readonly ILogger<PushService> _logger;
    private readonly IEmailSender _email;
    private readonly VapidDetails? _vapid;

    public PushService(
        ApplicationDbContext db,
        IRelogio relogio,
        IConfiguration configuration,
        ILogger<PushService> logger,
        IEmailSender email)
    {
        _db = db;
        _relogio = relogio;
        _logger = logger;
        _email = email;
        var pub = configuration["Push:VapidPublicKey"];
        var priv = configuration["Push:VapidPrivateKey"];
        var subj = configuration["Push:Subject"] ?? "mailto:premag@localhost";
        if (!string.IsNullOrWhiteSpace(pub) && !string.IsNullOrWhiteSpace(priv))
            _vapid = new VapidDetails(subj, pub, priv);
    }

    public string? ChavePublica => _vapid?.PublicKey;

    public async Task InscreverAsync(InscreverPushDto dto, UsuarioLogado quem, CancellationToken cancellationToken = default)
    {
        if (_vapid is null)
            throw new RegraNegocioException("PUSH_DESLIGADO", "Notificações push não estão configuradas neste ambiente.", 503);

        var endpoint = (dto.Endpoint ?? "").Trim();
        var p256 = (dto.P256dh ?? "").Trim();
        var auth = (dto.Auth ?? "").Trim();
        if (endpoint.Length < 10 || p256.Length == 0 || auth.Length == 0)
            throw new RegraNegocioException("PUSH_INSCRICAO_INVALIDA", "Inscrição de notificação incompleta.", 422);

        var existente = await _db.InscricoesPush
            .FirstOrDefaultAsync(i => i.Endpoint == endpoint, cancellationToken);

        if (existente is null)
        {
            _db.InscricoesPush.Add(new InscricaoPush
            {
                TenantId = _db.TenantId,
                UsuarioId = quem.Id,
                Endpoint = endpoint,
                P256dh = p256,
                Auth = auth,
                CriadoEm = _relogio.UtcAgora
            });
        }
        else
        {
            existente.UsuarioId = quem.Id;
            existente.P256dh = p256;
            existente.Auth = auth;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DesinscreverAsync(string endpoint, UsuarioLogado quem, CancellationToken cancellationToken = default)
    {
        var e = (endpoint ?? "").Trim();
        if (e.Length == 0)
            return;
        var lista = await _db.InscricoesPush
            .Where(i => i.Endpoint == e && i.UsuarioId == quem.Id)
            .ToListAsync(cancellationToken);
        _db.InscricoesPush.RemoveRange(lista);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task NotificarGestoresAsync(string titulo, string corpo, string url, CancellationToken cancellationToken = default) =>
        EnviarAsync(
            u => u.Perfil == Permissoes.Gerente || u.Perfil == Permissoes.Diretoria || u.Perfil == Permissoes.Admin,
            titulo, corpo, url, cancellationToken);

    public Task NotificarEquipeAsync(Guid equipeId, string titulo, string corpo, string url, CancellationToken cancellationToken = default) =>
        EnviarAsync(
            u => u.EquipeId == equipeId && u.Perfil == Permissoes.Encarregado,
            titulo, corpo, url, cancellationToken);

    private async Task EnviarAsync(
        Func<Usuario, bool> filtro,
        string titulo,
        string corpo,
        string url,
        CancellationToken cancellationToken)
    {
        var usuarios = await _db.Users.AsNoTracking()
            .Where(u => u.Ativo)
            .ToListAsync(cancellationToken);
        var alvo = usuarios.Where(filtro).ToList();
        if (alvo.Count == 0)
            return;

        var ids = alvo.Select(u => u.Id).ToHashSet();
        var okPush = new HashSet<Guid>();

        if (_vapid is not null)
        {
            var inscricoes = await _db.InscricoesPush
                .Where(i => ids.Contains(i.UsuarioId))
                .ToListAsync(cancellationToken);
            var payload = System.Text.Json.JsonSerializer.Serialize(new { title = titulo, body = corpo, url });
            var client = new WebPushClient();
            var mortas = new List<InscricaoPush>();

            foreach (var i in inscricoes)
            {
                try
                {
                    var sub = new PushSubscription(i.Endpoint, i.P256dh, i.Auth);
                    await client.SendNotificationAsync(sub, payload, _vapid);
                    i.UltimoEnvioEm = _relogio.UtcAgora;
                    okPush.Add(i.UsuarioId);
                }
                catch (WebPushException ex) when ((int)ex.StatusCode is 404 or 410)
                {
                    mortas.Add(i);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar push para {Endpoint}", i.Endpoint);
                }
            }

            if (mortas.Count > 0)
                _db.InscricoesPush.RemoveRange(mortas);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!_email.Configurado)
            return;

        foreach (var u in alvo.Where(u =>
                     (u.Perfil == Permissoes.Diretoria || u.Perfil == Permissoes.Admin)
                     && !okPush.Contains(u.Id)
                     && !string.IsNullOrWhiteSpace(u.Email)))
        {
            try
            {
                await _email.EnviarAsync(
                    u.Email!,
                    titulo,
                    $"{corpo}\n\nAbra o PREMAG: {url}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar e-mail para {Email}", u.Email);
            }
        }
    }
}
