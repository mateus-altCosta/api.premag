namespace Premag.Core.Entities;

public class AuditLog : EntidadeTenant
{
    public string Entidade { get; set; } = string.Empty;
    public Guid EntidadeId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string? Antes { get; set; }
    public string? Depois { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTimeOffset Em { get; set; }
    public string? Ip { get; set; }
}
