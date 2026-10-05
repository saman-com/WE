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
    public async Task CrossSchool_EvidenceCreatedConsumer_BothDirections_FactsStayIsolated()
    {
        // Consumer: consumers:evidence-created-edw
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var evidenceA = Guid.CreateVersion7();
        var evidenceB = Guid.CreateVersion7();

        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IEdwIngestProcessor>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<EdwDbContext>();

        await processor.ProcessEvidenceCreatedAsync(CreateEvidenceForTenant(schoolA, evidenceA));
        await processor.ProcessEvidenceCreatedAsync(CreateEvidenceForTenant(schoolB, evidenceB));

        tenantContext.SetTenant(schoolA);
        var factsA = await db.EvidenceFacts
            .Where(f => f.TenantId == tenantContext.TenantId)
            .ToListAsync();
        Assert.Single(factsA);
        Assert.Equal(schoolA, factsA[0].TenantId);
        Assert.Equal(evidenceA, factsA[0].EvidenceId);
        Assert.DoesNotContain(factsA, f => f.EvidenceId == evidenceB);

        tenantContext.SetTenant(schoolB);
        var factsB = await db.EvidenceFacts
            .Where(f => f.TenantId == tenantContext.TenantId)
            .ToListAsync();
        Assert.Single(factsB);
        Assert.Equal(schoolB, factsB[0].TenantId);
        Assert.Equal(evidenceB, factsB[0].EvidenceId);
        Assert.DoesNotContain(factsB, f => f.EvidenceId == evidenceA);
    }

    private static WePlatform.Events.EvidenceCreated CreateEvidenceForTenant(Guid organisationId, Guid evidenceId)
    {
        var eventId = Guid.CreateVersion7();
        return new WePlatform.Events.EvidenceCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            organisationId,
            WePlatform.Events.EvidenceCreated.CurrentVersion,
            evidenceId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new WePlatform.Events.MicroSkillResult(Guid.CreateVersion7(), 4, "Strong work.")]);
    }
}
