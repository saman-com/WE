using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Application.Auth;
using IdentityService.Domain;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Auth;
using IdentityService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WePlatform.AspNetCore;
using WePlatform.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityInfrastructure(builder.Configuration);

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
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddWePlatformCors(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.BackfillTenantIdsAsync<ApplicationUser>(_ => DefaultTenant.Id);
    var environment = app.Services.GetRequiredService<IHostEnvironment>();
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
    {
        await IdentityDataSeeder.SeedAsync(app.Services);
    }
}

app.UseWePlatformSecurityHeaders();
app.UseWePlatformCors();
app.UseAuthentication();
app.UseWePlatformTenancy();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/v1/auth/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    JwtTokenService tokenService) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
    {
        return Results.Json(
            new ApiErrorResponse("auth.invalid_credentials"),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var (token, expiresInSeconds) = await tokenService.CreateAccessTokenAsync(user);
    return Results.Ok(new LoginResponse(token, expiresInSeconds));
});

app.MapGet("/api/v1/auth/me", [Authorize] async (
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    ITenantContext tenantContext) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var user = await userManager.FindByIdAsync(userId);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, user);
    if (tenantAccess is not null)
    {
        return tenantAccess;
    }

    var roles = await userManager.GetRolesAsync(user);
    return Results.Ok(new UserProfileResponse(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.ToList()));
});

app.MapGet("/api/v1/auth/admin", [Authorize(Roles = PlatformRoles.SystemAdministrator)]
    () => Results.Ok(new { message = "admin access granted" }));

app.Run();

public partial class Program;
