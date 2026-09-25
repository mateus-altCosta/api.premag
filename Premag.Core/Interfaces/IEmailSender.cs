namespace Premag.Core.Interfaces;

/// <summary>Fallback de notificação para Diretoria quando o push não chega (memorial §6.1).</summary>
public interface IEmailSender
{
    bool Configurado { get; }
    Task EnviarAsync(string para, string assunto, string corpo, CancellationToken cancellationToken = default);
}
