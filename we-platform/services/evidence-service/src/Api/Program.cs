using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using EvidenceService.Api;
using EvidenceService.Domain;
using EvidenceService.Infrastructure;
using EvidenceService.Infrastructure.Data;
using WePlatform.Messaging;
using WePlatform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEvidenceInfrastructure(builder.Configuration, builder.Environment);

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
    var db = scope.ServiceProvider.GetRequiredService<EvidenceDbContext>();
    await db.Database.EnsureCreatedAsync();
    await MassTransitOutboxInboxSchema.EnsureTablesAsync(db);
    await db.BackfillTenantIdsAsync<EducationalEvidence>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<EvidenceMicroSkillMark>(m =>
    {
        var evidence = db.Evidence.IgnoreQueryFilters().FirstOrDefault(e => e.Id == m.EvidenceId);
        return evidence?.OrganisationId ?? DefaultTenant.Id;
    });
}

app.UseCors("WebPortal");
app.UseAuthentication();
app.UseWePlatformTenancy();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapEvidenceEndpoints();

app.Run();

public partial class Program;
