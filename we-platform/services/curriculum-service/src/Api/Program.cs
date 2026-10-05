using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CurriculumService.Api;
using CurriculumService.Domain;
using CurriculumService.Infrastructure;
using CurriculumService.Infrastructure.Data;
using WePlatform.AspNetCore;
using WePlatform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCurriculumInfrastructure(builder.Configuration);

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
    var db = scope.ServiceProvider.GetRequiredService<CurriculumDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.BackfillTenantIdsAsync<Curriculum>(TenantBackfill.ResolveOrganisationTenant);
    await db.BackfillTenantIdsAsync<Subject>(s =>
    {
        var curriculum = db.Curricula.IgnoreQueryFilters().FirstOrDefault(c => c.Id == s.CurriculumId);
        return curriculum?.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<Unit>(u =>
    {
        var subject = db.Subjects.IgnoreQueryFilters().Include(s => s.Curriculum)
            .FirstOrDefault(s => s.Id == u.SubjectId);
        return subject?.Curriculum.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<Topic>(t =>
    {
        var unit = db.Units.IgnoreQueryFilters().Include(u => u.Subject).ThenInclude(s => s.Curriculum)
            .FirstOrDefault(u => u.Id == t.UnitId);
        return unit?.Subject.Curriculum.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<LearningObjective>(o =>
    {
        var unit = db.Units.IgnoreQueryFilters().Include(u => u.Subject).ThenInclude(s => s.Curriculum)
            .FirstOrDefault(u => u.Id == o.UnitId);
        return unit?.Subject.Curriculum.OrganisationId ?? DefaultTenant.Id;
    });
    await db.BackfillTenantIdsAsync<MicroSkill>(m =>
    {
        var objective = db.LearningObjectives.IgnoreQueryFilters()
            .Include(o => o.Unit).ThenInclude(u => u.Subject).ThenInclude(s => s.Curriculum)
            .FirstOrDefault(o => o.Id == m.LearningObjectiveId);
        return objective?.Unit.Subject.Curriculum.OrganisationId ?? DefaultTenant.Id;
    });
}

app.UseWePlatformSecurityHeaders();
app.UseWePlatformCors();
app.UseAuthentication();
app.UseWePlatformTenancy();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapCurriculumEndpoints();

app.Run();

public partial class Program;
