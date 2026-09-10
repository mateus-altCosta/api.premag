namespace Premag.Core.Entities;

public abstract class EntidadeTenant
{
    public Guid Id { get; set; } = GeradorId.Novo();
    public Guid TenantId { get; set; }
}
