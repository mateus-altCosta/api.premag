using Premag.Core.Interfaces;

namespace Premag.API.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var raw = context.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(raw, out var tenantId))
            tenantContext.Definir(tenantId);

        await _next(context);
    }
}
