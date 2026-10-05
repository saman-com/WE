using System.Text;
using ReportingService.Api;
using ReportingService.Domain;
using ReportingService.Infrastructure;
using ReportingService.Infrastructure.Data;
using ReportingService.Infrastructure.Data.Edw;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReportingInfrastructure(builder.Configuration, builder.Environment);

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT signing key is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebPortal", policy =>
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.BackfillTenantIdsAsync<GeneratedReport>(TenantBackfill.ResolveOrganisationTenant);

    var edwDb = scope.ServiceProvider.GetRequiredService<EdwAnalyticsDbContext>();
    await edwDb.Database.EnsureCreatedAsync();
    // edw-ingest may EnsureCreated first without effectiveness columns; apply V002 idempotently.
    await edwDb.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE fact_evidence
            ADD COLUMN IF NOT EXISTS subject_id UUID,
            ADD COLUMN IF NOT EXISTS subject_name VARCHAR(256),
            ADD COLUMN IF NOT EXISTS unit_id UUID,
            ADD COLUMN IF NOT EXISTS unit_name VARCHAR(256),
            ADD COLUMN IF NOT EXISTS mastered_micro_skill_count INTEGER NOT NULL DEFAULT 0,
            ADD COLUMN IF NOT EXISTS total_micro_skill_count INTEGER NOT NULL DEFAULT 0;
        CREATE INDEX IF NOT EXISTS idx_fact_evidence_subject_id ON fact_evidence (subject_id);
        CREATE INDEX IF NOT EXISTS idx_fact_evidence_unit_id ON fact_evidence (unit_id);
        ALTER TABLE fact_intervention
            ADD COLUMN IF NOT EXISTS intervention_type VARCHAR(64);
        """);
    await edwDb.BackfillTenantIdsAsync<EdwDimTime>(_ => DefaultTenant.Id);
    await edwDb.BackfillTenantIdsAsync<EdwEvidenceFact>(TenantBackfill.ResolveOrganisationTenant);
    await edwDb.BackfillTenantIdsAsync<EdwInterventionFact>(TenantBackfill.ResolveOrganisationTenant);
}

app.UseCors("WebPortal");
app.UseAuthentication();
app.UseWePlatformTenancy();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapReportEndpoints();
app.MapLongitudinalAnalyticsEndpoints();
app.MapEffectivenessAnalyticsEndpoints();

app.Run();

public partial class Program;
