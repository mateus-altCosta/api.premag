using Premag.Core.Interfaces;

namespace Premag.Infrastructure.Tenancy;

public sealed class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<Guid> Atual = new();

    public Guid TenantId => Atual.Value;
    public bool TemTenant => Atual.Value != Guid.Empty;
    public void Definir(Guid tenantId) => Atual.Value = tenantId;
}
