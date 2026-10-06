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
    if (db.Database.IsRelational())
    {
        await db.Database.ExecuteSqlRawAsync(
            """ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS federation_id uuid NULL""");
        await db.Database.ExecuteSqlRawAsync(
            """ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true""");
    }

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

    if (!user.IsActive)
    {
        return Results.Json(
            new ApiErrorResponse("auth.account_inactive"),
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

app.MapGet("/api/v1/parents/me/teachers", [Authorize(Roles = PlatformRoles.Parent)] async (
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IParentClassTeacherSource teacherSource,
    HttpContext httpContext) =>
{
    if (tenantContext.TenantId is null)
    {
        return Results.Forbid();
    }

    var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
    if (bearerToken is null)
    {
        return Results.Forbid();
    }

    var teacherIds = await teacherSource.ListTeacherUserIdsAsync(bearerToken, httpContext.RequestAborted);
    var allowedIds = teacherIds
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.Ordinal)
        .ToList();
    if (allowedIds.Count == 0)
    {
        return Results.Ok(Array.Empty<ParentTeacherResponse>());
    }

    var users = await userManager.Users
        .Where(user => user.TenantId == tenantContext.TenantId && allowedIds.Contains(user.Id))
        .ToListAsync();

    var teachers = new List<ParentTeacherResponse>();
    foreach (var user in users.OrderBy(user => user.DisplayName, StringComparer.Ordinal))
    {
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(PlatformRoles.Teacher))
        {
            continue;
        }

        teachers.Add(new ParentTeacherResponse(user.Id, user.DisplayName));
    }

    return Results.Ok(teachers);
});

app.MapGet("/api/v1/parents/me/children-names", [Authorize(Roles = PlatformRoles.Parent)] async (
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager,
    IParentClassTeacherSource teacherSource,
    HttpContext httpContext) =>
{
    if (tenantContext.TenantId is null)
    {
        return Results.Forbid();
    }

    var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
    if (bearerToken is null)
    {
        return Results.Forbid();
    }

    var childIds = await teacherSource.ListLinkedStudentUserIdsAsync(bearerToken, httpContext.RequestAborted);
    var allowedIds = childIds
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Distinct(StringComparer.Ordinal)
        .ToList();
    if (allowedIds.Count == 0)
    {
        return Results.Ok(Array.Empty<ParentChildNameResponse>());
    }

    var users = await userManager.Users
        .Where(user => user.TenantId == tenantContext.TenantId && allowedIds.Contains(user.Id))
        .ToListAsync();

    var children = new List<ParentChildNameResponse>();
    foreach (var user in users.OrderBy(user => user.DisplayName, StringComparer.Ordinal))
    {
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(PlatformRoles.Student))
        {
            continue;
        }

        children.Add(new ParentChildNameResponse(user.Id, user.DisplayName));
    }

    return Results.Ok(children);
});

app.MapGet("/api/v1/users", [Authorize(Roles = $"{PlatformRoles.SystemAdministrator},{PlatformRoles.FederationAdmin},{PlatformRoles.Teacher},{PlatformRoles.Student},{PlatformRoles.SchoolLeader}")] async (
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) =>
{
    if (tenantContext.TenantId is null)
    {
        return Results.Forbid();
    }

    var users = await userManager.Users
        .Where(user => user.TenantId == tenantContext.TenantId)
        .OrderBy(user => user.DisplayName)
        .ToListAsync();

    var directory = new List<DirectoryUserResponse>();
    foreach (var user in users)
    {
        var roles = await userManager.GetRolesAsync(user);
        directory.Add(new DirectoryUserResponse(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            roles.ToList(),
            user.IsActive));
    }

    return Results.Ok(directory);
});

app.MapPost("/api/v1/users", [Authorize(Roles = PlatformRoles.SystemAdministrator)] async (
    CreateUserRequest request,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) =>
{
    if (tenantContext.TenantId is null)
    {
        return Results.Forbid();
    }

    var email = request.Email?.Trim() ?? string.Empty;
    var name = request.Name?.Trim() ?? string.Empty;
    var role = request.Role?.Trim() ?? string.Empty;
    if (email.Length == 0
        || name.Length == 0
        || string.IsNullOrEmpty(request.Password)
        || !PlatformRoles.All.Contains(role))
    {
        return Results.Json(new ApiErrorResponse("users.invalid"), statusCode: StatusCodes.Status400BadRequest);
    }

    if (await userManager.FindByEmailAsync(email) is not null)
    {
        return Results.Json(new ApiErrorResponse("users.email_taken"), statusCode: StatusCodes.Status409Conflict);
    }

    var user = new ApplicationUser
    {
        Id = Guid.NewGuid().ToString(),
        UserName = email,
        Email = email,
        DisplayName = name,
        EmailConfirmed = true,
        TenantId = tenantContext.TenantId.Value,
        FederationId = role == PlatformRoles.FederationAdmin
            ? await userManager.Users
                .Where(existing => existing.TenantId == tenantContext.TenantId && existing.FederationId != null)
                .Select(existing => existing.FederationId)
                .FirstOrDefaultAsync()
            : null
    };

    if (role == PlatformRoles.FederationAdmin && user.FederationId is null)
    {
        return Results.Json(new ApiErrorResponse("users.invalid"), statusCode: StatusCodes.Status400BadRequest);
    }

    var created = await userManager.CreateAsync(user, request.Password);
    if (!created.Succeeded)
    {
        return IdentityUserError(created);
    }

    var roleResult = await userManager.AddToRoleAsync(user, role);
    if (!roleResult.Succeeded)
    {
        await userManager.DeleteAsync(user);
        return IdentityUserError(roleResult);
    }

    return Results.Ok(new DirectoryUserResponse(user.Id, user.DisplayName, user.Email ?? email, [role], user.IsActive));
});

app.MapPut("/api/v1/users/{userId}", [Authorize(Roles = PlatformRoles.SystemAdministrator)] async (
    string userId,
    UpdateUserRequest request,
    ClaimsPrincipal principal,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) =>
{
    var (user, error) = await FindManagedUserAsync(userId, tenantContext, userManager);
    if (error is not null || user is null)
    {
        return error ?? Results.NotFound();
    }

    var name = request.Name?.Trim() ?? string.Empty;
    var role = request.Role?.Trim() ?? string.Empty;
    if (name.Length == 0 || !PlatformRoles.All.Contains(role))
    {
        return Results.Json(new ApiErrorResponse("users.invalid"), statusCode: StatusCodes.Status400BadRequest);
    }

    var currentRoles = await userManager.GetRolesAsync(user);
    var callerId = CallerUserId(principal);
    if (callerId == user.Id
        && currentRoles.Contains(PlatformRoles.SystemAdministrator)
        && role != PlatformRoles.SystemAdministrator)
    {
        return Results.Json(
            new ApiErrorResponse("users.cannot_remove_own_admin"),
            statusCode: StatusCodes.Status400BadRequest);
    }

    if (role == PlatformRoles.FederationAdmin)
    {
        user.FederationId ??= await userManager.Users
            .Where(existing => existing.TenantId == user.TenantId && existing.FederationId != null)
            .Select(existing => existing.FederationId)
            .FirstOrDefaultAsync();
        if (user.FederationId is null)
        {
            return Results.Json(new ApiErrorResponse("users.invalid"), statusCode: StatusCodes.Status400BadRequest);
        }
    }
    else
    {
        user.FederationId = null;
    }

    user.DisplayName = name;
    var updated = await userManager.UpdateAsync(user);
    if (!updated.Succeeded)
    {
        return IdentityUserError(updated);
    }

    foreach (var existing in currentRoles)
    {
        if (existing == role)
        {
            continue;
        }

        var removed = await userManager.RemoveFromRoleAsync(user, existing);
        if (!removed.Succeeded)
        {
            return IdentityUserError(removed);
        }
    }

    if (!currentRoles.Contains(role))
    {
        var added = await userManager.AddToRoleAsync(user, role);
        if (!added.Succeeded)
        {
            return IdentityUserError(added);
        }
    }

    return Results.Ok(new DirectoryUserResponse(user.Id, user.DisplayName, user.Email ?? string.Empty, [role], user.IsActive));
});

app.MapPost("/api/v1/users/{userId}/deactivate", [Authorize(Roles = PlatformRoles.SystemAdministrator)] async (
    string userId,
    ClaimsPrincipal principal,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) =>
{
    var (user, error) = await FindManagedUserAsync(userId, tenantContext, userManager);
    if (error is not null || user is null)
    {
        return error ?? Results.NotFound();
    }

    if (CallerUserId(principal) == user.Id)
    {
        return Results.Json(
            new ApiErrorResponse("users.cannot_deactivate_self"),
            statusCode: StatusCodes.Status400BadRequest);
    }

    user.IsActive = false;
    var updated = await userManager.UpdateAsync(user);
    if (!updated.Succeeded)
    {
        return IdentityUserError(updated);
    }

    var roles = await userManager.GetRolesAsync(user);
    return Results.Ok(new DirectoryUserResponse(user.Id, user.DisplayName, user.Email ?? string.Empty, roles.ToList(), user.IsActive));
});

app.MapPost("/api/v1/users/{userId}/reactivate", [Authorize(Roles = PlatformRoles.SystemAdministrator)] async (
    string userId,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) =>
{
    var (user, error) = await FindManagedUserAsync(userId, tenantContext, userManager);
    if (error is not null || user is null)
    {
        return error ?? Results.NotFound();
    }

    user.IsActive = true;
    var updated = await userManager.UpdateAsync(user);
    if (!updated.Succeeded)
    {
        return IdentityUserError(updated);
    }

    var roles = await userManager.GetRolesAsync(user);
    return Results.Ok(new DirectoryUserResponse(user.Id, user.DisplayName, user.Email ?? string.Empty, roles.ToList(), user.IsActive));
});

app.MapPost("/api/v1/users/{userId}/password", [Authorize(Roles = PlatformRoles.SystemAdministrator)] async (
    string userId,
    ResetPasswordRequest request,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager) =>
{
    var (user, error) = await FindManagedUserAsync(userId, tenantContext, userManager);
    if (error is not null || user is null)
    {
        return error ?? Results.NotFound();
    }

    if (string.IsNullOrEmpty(request.Password))
    {
        return Results.Json(new ApiErrorResponse("users.password_invalid"), statusCode: StatusCodes.Status400BadRequest);
    }

    if (await userManager.HasPasswordAsync(user))
    {
        var removed = await userManager.RemovePasswordAsync(user);
        if (!removed.Succeeded)
        {
            return IdentityUserError(removed);
        }
    }

    var reset = await userManager.AddPasswordAsync(user, request.Password);
    if (!reset.Succeeded)
    {
        return IdentityUserError(reset);
    }

    var roles = await userManager.GetRolesAsync(user);
    return Results.Ok(new DirectoryUserResponse(user.Id, user.DisplayName, user.Email ?? string.Empty, roles.ToList(), user.IsActive));
});

app.Run();

static async Task<(ApplicationUser? User, IResult? Error)> FindManagedUserAsync(
    string userId,
    ITenantContext tenantContext,
    UserManager<ApplicationUser> userManager)
{
    if (tenantContext.TenantId is null)
    {
        return (null, Results.Forbid());
    }

    var user = await userManager.FindByIdAsync(userId);
    if (user is null)
    {
        return (null, Results.NotFound());
    }

    var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, user);
    if (tenantAccess is not null)
    {
        return (null, tenantAccess);
    }

    return (user, null);
}

static string? CallerUserId(ClaimsPrincipal principal) =>
    principal.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

static string? ExtractBearerToken(string authorizationHeader)
{
    const string prefix = "Bearer ";
    if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    var token = authorizationHeader[prefix.Length..].Trim();
    return string.IsNullOrWhiteSpace(token) ? null : token;
}

static IResult IdentityUserError(IdentityResult result)
{
    var duplicate = result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail");
    if (duplicate)
    {
        return Results.Json(new ApiErrorResponse("users.email_taken"), statusCode: StatusCodes.Status409Conflict);
    }

    var passwordFailed = result.Errors.Any(error => error.Code.StartsWith("Password", StringComparison.Ordinal));
    var code = passwordFailed ? "users.password_invalid" : "users.invalid";
    return Results.Json(new ApiErrorResponse(code), statusCode: StatusCodes.Status400BadRequest);
}

public partial class Program;
