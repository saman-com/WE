using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

namespace IdentityService.Tests;

internal static class TestJwt
{
    public static string Create(string userId, Guid tenantId, params string[] roles) =>
        Create(userId, tenantId, DateTime.UtcNow.AddMinutes(15), roles);

    /// <summary>
    /// Builds a token that is already expired past the default JWT clock skew (5 minutes).
    /// </summary>
    public static string CreateExpired(string userId, Guid tenantId, params string[] roles) =>
        Create(userId, tenantId, DateTime.UtcNow.AddMinutes(-10), roles);

    private static string Create(string userId, Guid tenantId, DateTime expiresUtc, string[] roles)
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
            expires: expiresUtc,
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
