using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FederationService.Application;
using FederationService.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WePlatform.Tenancy;

namespace FederationService.Infrastructure;

public sealed class HttpRegionalConfigurationProvisioner(
    HttpClient client,
    IConfiguration configuration) : IRegionalConfigurationProvisioner
{
    public async Task ProvisionDefaultConfigurationAsync(
        Guid schoolTenantId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["ConfigurationService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        if (client.BaseAddress is null)
        {
            client.BaseAddress = new Uri(baseUrl);
        }

        var requestBody = new
        {
            academicCalendar = new
            {
                terms = new[]
                {
                    new { name = "Term 1", startDate = "2026-02-01", endDate = "2026-04-15" }
                },
                holidays = Array.Empty<object>()
            },
            gradingScale = new
            {
                name = "Default",
                levels = new[]
                {
                    new { label = "A", minScore = 85m, maxScore = 100m }
                }
            },
            assessmentModels = new[]
            {
                new { name = "Ongoing Checks", category = "Formative" }
            },
            reportingTemplates = new[]
            {
                new { id = "term-summary", name = "Term Summary", format = "pdf" }
            },
            localeSettings = new
            {
                languageCode = "en",
                regionCode = "US",
                dateFormat = "yyyy-MM-dd",
                timeZone = "UTC"
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/regional-configuration?tenantId={schoolTenantId}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateSchoolTenantToken(schoolTenantId));
        request.Content = JsonContent.Create(requestBody);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private string CreateSchoolTenantToken(Guid schoolTenantId)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(issuer)
            || string.IsNullOrWhiteSpace(audience)
            || string.IsNullOrWhiteSpace(key))
        {
            throw new HttpRequestException("Configuration service credentials are not configured.");
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "federation-service"),
            new Claim(ClaimTypes.NameIdentifier, "federation-service"),
            new Claim(ClaimTypes.Role, PlatformRoles.SystemAdministrator),
            new Claim(TenantClaimTypes.TenantId, schoolTenantId.ToString())
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
