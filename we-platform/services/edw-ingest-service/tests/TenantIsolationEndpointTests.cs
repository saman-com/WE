using EdwIngestService.Application;
using EdwIngestService.Infrastructure;
using EdwIngestService.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace EdwIngestService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TenantIsolationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task EvidenceFacts_AreScopedToTenantContext()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();

        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IEdwIngestProcessor>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<EdwDbContext>();

        await processor.ProcessEvidenceCreatedAsync(CreateEvidenceForTenant(tenantA));
        await processor.ProcessEvidenceCreatedAsync(CreateEvidenceForTenant(tenantB));

        tenantContext.SetTenant(tenantA);
        var tenantAFacts = await db.EvidenceFacts
            .Where(f => f.TenantId == tenantContext.TenantId)
            .ToListAsync();
        Assert.Single(tenantAFacts);
        Assert.Equal(tenantA, tenantAFacts[0].TenantId);

        tenantContext.SetTenant(tenantB);
        var tenantBFacts = await db.EvidenceFacts
            .Where(f => f.TenantId == tenantContext.TenantId)
            .ToListAsync();
        Assert.Single(tenantBFacts);
        Assert.Equal(tenantB, tenantBFacts[0].TenantId);
    }

    private static WePlatform.Events.EvidenceCreated CreateEvidenceForTenant(Guid organisationId)
    {
        var eventId = Guid.CreateVersion7();
        return new WePlatform.Events.EvidenceCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            organisationId,
            WePlatform.Events.EvidenceCreated.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new WePlatform.Events.MicroSkillResult(Guid.CreateVersion7(), 4, "Strong work.")]);
    }
}
