using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

namespace EvidenceApprovalFlow.Tests;

internal static class TestJwt
{
    public const string Issuer = "we-platform-identity-test";
    public const string Audience = "we-platform-test";
    public const string Key = "test-signing-key-at-least-32-chars-long";
    public const string TeacherRole = "Teacher";
    public const string AdminRole = "SystemAdministrator";

    public static string Create(string userId, Guid tenantId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(TenantClaimTypes.TenantId, tenantId.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
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
