using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using NationalReportingService.Api;
using NationalReportingService.Api.Auth;
using NationalReportingService.Infrastructure;
using NationalReportingService.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNationalReportingInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme,
        _ => { });

builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
builder.Services.AddAuthorization(options => options.AddNationalScopePolicies());

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
    var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseCors("WebPortal");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapOpenApi("/api/v1/docs");
app.MapNationalReportingEndpoints();

app.Run();

public partial class Program;
