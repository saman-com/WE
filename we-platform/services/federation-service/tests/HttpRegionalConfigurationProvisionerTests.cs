using System.Net;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FederationService.Infrastructure;
using Microsoft.Extensions.Configuration;
using WePlatform.Tenancy;

namespace FederationService.Tests;

public class HttpRegionalConfigurationProvisionerTests
{
    [Fact]
    public async Task ProvisionDefaultConfiguration_AuthenticatesAsTheNewSchoolTenant()
    {
        HttpRequestMessage? captured = null;
        var handler = new RecordingHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConfigurationService:BaseUrl"] = "http://configuration-service:8080",
                ["Jwt:Issuer"] = "we-platform-identity",
                ["Jwt:Audience"] = "we-platform",
                ["Jwt:Key"] = "local-dev-only-change-in-production-32chars"
            })
            .Build();
        var provisioner = new HttpRegionalConfigurationProvisioner(client, configuration);
        var tenantId = Guid.CreateVersion7();

        await provisioner.ProvisionDefaultConfigurationAsync(tenantId);

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Put, captured.Method);
        Assert.Contains($"tenantId={tenantId}", captured.RequestUri?.Query, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(captured.Headers.Authorization);
        Assert.Equal("Bearer", captured.Headers.Authorization.Scheme);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(captured.Headers.Authorization.Parameter);
        Assert.Equal(
            tenantId.ToString(),
            jwt.Claims.Single(claim => claim.Type == TenantClaimTypes.TenantId).Value);
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == ClaimTypes.Role && claim.Value == "SystemAdministrator");
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
