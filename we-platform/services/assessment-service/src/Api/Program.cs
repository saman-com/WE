using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AssessmentService.Api;
using AssessmentService.Domain;
using AssessmentService.Infrastructure;
using AssessmentService.Infrastructure.Data;
using WePlatform.AspNetCore;
using WePlatform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAssessmentInfrastructure(builder.Configuration, builder.Environment);

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
    var db = scope.ServiceProvider.GetRequiredService<AssessmentDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.BackfillTenantIdsAsync<Assessment>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<AssessmentSubmission>(s =>
    {
        var assessment = db.Assessments.IgnoreQueryFilters().FirstOrDefault(a => a.Id == s.AssessmentId);
        return assessment?.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<AssessmentLearningObjective>(l =>
    {
        var assessment = db.Assessments.IgnoreQueryFilters().FirstOrDefault(a => a.Id == l.AssessmentId);
        return assessment?.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<AssessmentMicroSkill>(m =>
    {
        var assessment = db.Assessments.IgnoreQueryFilters().FirstOrDefault(a => a.Id == m.AssessmentId);
        return assessment?.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<AiFeedbackAuditLog>(a =>
    {
        var assessment = db.Assessments.IgnoreQueryFilters().FirstOrDefault(x => x.Id == a.AssessmentId);
        return assessment?.OrganisationId ?? DefaultTenant.Id;
    });
}

app.UseWePlatformSecurityHeaders();
app.UseWePlatformCors();
app.UseAuthentication();
app.UseWePlatformTenancy();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapAssessmentEndpoints();

app.Run();

public partial class Program;
