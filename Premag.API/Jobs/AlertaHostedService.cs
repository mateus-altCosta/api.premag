using Microsoft.EntityFrameworkCore;
using Premag.Application.Interfaces.Services;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;

namespace Premag.API.Jobs;

/// <summary>RN-07: detecta ociosidade e demais alertas no servidor a cada 5 minutos, por tenant.</summary>
public sealed class AlertaHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<AlertaHostedService> _logger;

    public AlertaHostedService(IServiceScopeFactory escopos, ILogger<AlertaHostedService> logger)
    {
        _escopos = escopos;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RodarAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RodarAsync(stoppingToken);
    }

    private async Task RodarAsync(CancellationToken stoppingToken)
    {
        try
        {
            List<Guid> tenants;
            using (var scope = _escopos.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                tenants = await db.Tenants.Where(t => t.Ativo).Select(t => t.Id).ToListAsync(stoppingToken);
            }

            foreach (var id in tenants)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;
                using var scope = _escopos.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tenant.Definir(id);
                var svc = scope.ServiceProvider.GetRequiredService<IOcorrenciaService>();
                await svc.DetectarAgoraAsync(stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao detectar alertas.");
        }
    }
}
