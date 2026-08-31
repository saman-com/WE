using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FederationService.Application;
using FederationService.Domain;
using FederationService.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

namespace FederationService.Tests;

internal static class TestJwt
{
    public const string FederationAdminRole = PlatformRoles.FederationAdmin;
    public const string SchoolAdminRole = PlatformRoles.SystemAdministrator;

    public static string CreateForFederationAdmin(string userId, Guid federationId) =>
        Create(userId, federationId, null, FederationAdminRole);

    public static string CreateForSchoolAdmin(string userId, Guid tenantId) =>
        Create(userId, null, tenantId, SchoolAdminRole);

    public static string Create(string userId, Guid? federationId, Guid? tenantId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId)
        };

        if (federationId.HasValue)
        {
            claims.Add(new Claim(FederationClaimTypes.FederationId, federationId.Value.ToString()));
        }

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(TenantClaimTypes.TenantId, tenantId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-signing-key-at-least-32-chars-long"));
        var token = new JwtSecurityToken(
            issuer: "we-platform-identity-test",
            audience: "we-platform-test",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpRequestMessage FederationAuthorized(HttpMethod method, string url, string userId, Guid federationId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateForFederationAdmin(userId, federationId));
        return request;
    }

    public static HttpRequestMessage SchoolAdminAuthorized(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateForSchoolAdmin(userId, tenantId));
        return request;
    }
}

public sealed class FakeRegionalConfigurationProvisioner : IRegionalConfigurationProvisioner
{
    public List<Guid> ProvisionedTenantIds { get; } = [];

    public Task ProvisionDefaultConfigurationAsync(Guid schoolTenantId, CancellationToken cancellationToken = default)
    {
        ProvisionedTenantIds.Add(schoolTenantId);
        return Task.CompletedTask;
    }
}

public sealed class FederationWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeRegionalConfigurationProvisioner Provisioner { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IRegionalConfigurationProvisioner>();
            services.AddSingleton<IRegionalConfigurationProvisioner>(Provisioner);
        });
    }
}
