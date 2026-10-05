using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using NationalReportingService.Api;
using NationalReportingService.Api.Auth;
using NationalReportingService.Infrastructure;
using NationalReportingService.Infrastructure.Data;
using WePlatform.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNationalReportingInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer<CountCellSchemaTransformer>();
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT signing key is not configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
        options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme,
        _ => { })
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

builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddNationalScopePolicies();
    options.AddEducationAuthorityOfficerPolicy();
});

var rateLimitPermitLimit = builder.Configuration.GetValue("NationalReporting:RateLimitPermitLimit", 60);
var rateLimitWindowSeconds = builder.Configuration.GetValue("NationalReporting:RateLimitWindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("national-api", httpContext =>
    {
        var apiKey = httpContext.Request.Headers[ApiKeyAuthenticationOptions.HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = "anonymous";
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            apiKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddWePlatformCors(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseWePlatformSecurityHeaders();
app.UseWePlatformCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapOpenApi("/api/v1/docs");
app.MapNationalReportingEndpoints();
app.MapPolicyDashboardEndpoints();

app.Run();

public partial class Program;
