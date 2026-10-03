using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NationalReportingService.Infrastructure.Data;
using NationalReportingService.Infrastructure.Security;

namespace NationalReportingService.Api.Auth;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    public const string ClientNameClaimType = "ministry_client";
    public const string ScopeClaimType = "scope";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var headerValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.Fail("API key was empty.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
        var keyHash = ApiKeyHasher.Hash(apiKey.Trim());

        var ministryKey = await db.MinistryApiKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

        if (ministryKey is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, ministryKey.Id.ToString()),
            new(ClientNameClaimType, ministryKey.ClientName),
            new(ClaimTypes.Name, ministryKey.ClientName)
        };

        foreach (var scopeValue in ministryKey.Scopes.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ScopeClaimType, scopeValue));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
