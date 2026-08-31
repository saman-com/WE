using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

namespace IdentityService.Tests;

internal static class TestJwt
{
    public static string Create(string userId, Guid tenantId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(TenantClaimTypes.TenantId, tenantId.ToString())
        };
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

    public static HttpRequestMessage Authorized(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        params string[] roles)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Create(userId, tenantId, roles));
        return request;
    }
}
