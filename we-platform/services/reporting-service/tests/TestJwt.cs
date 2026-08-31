using System.Text;
using ReportingService.Application;
using WePlatform.Tenancy;

namespace ReportingService.Tests;

internal static class TestJwt
{
    public const string AdminRole = "SystemAdministrator";
    public const string TeacherRole = "Teacher";
    public const string SchoolLeaderRole = "SchoolLeader";
    public const string StudentRole = "Student";

    public static string Create(string userId, params string[] roles) =>
        Create(userId, DefaultTenant.Id, roles);

    public static string Create(string userId, Guid tenantId, params string[] roles)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, userId),
            new(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
            new(TenantClaimTypes.TenantId, tenantId.ToString())
        };
        claims.AddRange(roles.Select(role => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)));

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("test-signing-key-at-least-32-chars-long"));
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "we-platform-identity-test",
            audience: "we-platform-test",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256));

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpRequestMessage Authorized(HttpMethod method, string url, string userId, params string[] roles) =>
        Authorized(method, url, userId, DefaultTenant.Id, roles);

    public static HttpRequestMessage Authorized(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        params string[] roles)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            Create(userId, tenantId, roles));
        return request;
    }
}
