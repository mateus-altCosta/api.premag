using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Premag.Core.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Premag.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly string? _host;
    private readonly int _porta;
    private readonly bool _ssl;
    private readonly string? _usuario;
    private readonly string? _senha;
    private readonly string _de;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;
        _host = configuration["Email:Host"];
        _porta = int.TryParse(configuration["Email:Port"], out var p) ? p : 587;
        _ssl = !string.Equals(configuration["Email:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);
        _usuario = configuration["Email:User"];
        _senha = configuration["Email:Password"];
        _de = configuration["Email:From"] ?? _usuario ?? "premag@localhost";
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_host);

    public async Task EnviarAsync(string para, string assunto, string corpo, CancellationToken cancellationToken = default)
    {
        if (!Configurado || string.IsNullOrWhiteSpace(para))
            return;

        using var client = new SmtpClient(_host, _porta)
        {
            EnableSsl = _ssl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrWhiteSpace(_usuario))
            client.Credentials = new NetworkCredential(_usuario, _senha);

        using var msg = new MailMessage(_de, para.Trim(), assunto, corpo);
        await client.SendMailAsync(msg, cancellationToken);
        _logger.LogInformation("E-mail enviado para {Para}", para);
    }
}
