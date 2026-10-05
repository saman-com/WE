using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

namespace AssessmentService.Infrastructure.Ai;

public sealed class ServiceJwtIssuer(IConfiguration configuration)
{
    public string CreatePlatformServiceToken(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required for AI gateway service tokens.", nameof(tenantId));
        }

        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"]
            ?? throw new InvalidOperationException("JWT issuer is not configured.");
        var audience = jwtSection["Audience"]
            ?? throw new InvalidOperationException("JWT audience is not configured.");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "assessment-service"),
            new Claim(ClaimTypes.NameIdentifier, "assessment-service"),
            new Claim(ClaimTypes.Role, "PlatformService"),
            new Claim(TenantClaimTypes.TenantId, tenantId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
