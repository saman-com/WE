using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

namespace IdentityService.Infrastructure.Auth;

public sealed class JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
{
    public async Task<(string Token, int ExpiresInSeconds)> CreateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName),
            new(TenantClaimTypes.TenantId, user.TenantId.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSigningKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresInSeconds = 900;

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresInSeconds);
    }

    private string GetSigningKey() =>
        configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT signing key is not configured.");
}
