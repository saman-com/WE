using System.Net.Http.Json;
using FederationService.Application;
using Microsoft.Extensions.Configuration;

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

        var request = new
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

        using var response = await client.PutAsJsonAsync(
            $"/api/v1/regional-configuration?tenantId={schoolTenantId}",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
