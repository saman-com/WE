using System.IdentityModel.Tokens.Jwt;
using AssessmentService.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using WePlatform.Tenancy;

namespace AssessmentService.Tests;

public class ServiceJwtIssuerTests
{
    [Fact]
    public void CreatePlatformServiceToken_IncludesRequestTenantClaim()
    {
        var schoolBTenantId = Guid.Parse("00000000-0000-4000-8000-000000000002");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "we-platform-identity-test",
                ["Jwt:Audience"] = "we-platform-test",
                ["Jwt:Key"] = "test-signing-key-at-least-32-chars-long"
            })
            .Build();

        var issuer = new ServiceJwtIssuer(configuration);
        var token = issuer.CreatePlatformServiceToken(schoolBTenantId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var tenantClaim = jwt.Claims.Single(claim => claim.Type == TenantClaimTypes.TenantId);

        Assert.Equal(schoolBTenantId.ToString(), tenantClaim.Value);
        Assert.NotEqual(DefaultTenant.Id.ToString(), tenantClaim.Value);
    }
}
