using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrganisationService.Api;
using OrganisationService.Domain;
using OrganisationService.Infrastructure;
using OrganisationService.Infrastructure.Data;
using WePlatform.AspNetCore;
using WePlatform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    DownstreamServiceUrls.RequireConfigured(builder.Configuration);
}

builder.Services.AddOrganisationInfrastructure(builder.Configuration);

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

builder.Services.AddWePlatformCors(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrganisationDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.BackfillTenantIdsAsync<Organisation>(e => TenantBackfill.ResolveSelfTenant(e.Id));
    await db.BackfillTenantIdsAsync<YearLevel>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<SchoolClass>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<OrganisationLeader>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<ClassTeacher>(ct =>
    {
        var schoolClass = db.Classes.IgnoreQueryFilters().FirstOrDefault(c => c.Id == ct.ClassId);
        return schoolClass?.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<ClassEnrollment>(ce =>
    {
        var schoolClass = db.Classes.IgnoreQueryFilters().FirstOrDefault(c => c.Id == ce.ClassId);
        return schoolClass?.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<ParentStudentLink>(_ => DefaultTenant.Id);
}

app.UseWePlatformSecurityHeaders();
app.UseWePlatformCors();
app.UseAuthentication();
app.UseWePlatformTenancy();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapOrganisationEndpoints();
app.MapLeadershipDashboardEndpoints();
app.MapStudentWorkspaceEndpoints();
app.MapParentWorkspaceEndpoints();

app.Run();

public partial class Program;
