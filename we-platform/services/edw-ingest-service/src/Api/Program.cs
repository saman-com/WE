using EdwIngestService.Domain;
using EdwIngestService.Infrastructure;
using EdwIngestService.Infrastructure.Data;
using WePlatform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEdwIngestInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EdwDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.BackfillTenantIdsAsync<DimTime>(_ => DefaultTenant.Id);
    await db.BackfillTenantIdsAsync<EvidenceFact>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<AssessmentFact>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<InterventionFact>(TenantBackfill.ResolveOrganisationTenant);
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
