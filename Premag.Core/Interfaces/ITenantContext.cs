namespace Premag.Core.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool TemTenant { get; }
    void Definir(Guid tenantId);
}
