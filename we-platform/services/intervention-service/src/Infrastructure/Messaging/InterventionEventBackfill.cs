using InterventionService.Application;
using InterventionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InterventionService.Infrastructure.Messaging;

public sealed class InterventionEventBackfill(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InterventionDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IInterventionEventPublisher>();
        var rows = await db.Interventions.IgnoreQueryFilters().ToListAsync(stoppingToken);
        foreach (var row in rows)
        {
            await publisher.PublishAsync(row, stoppingToken);
        }
    }
}
