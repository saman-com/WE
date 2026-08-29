using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrganisationService.Application;

namespace OrganisationService.Infrastructure.Evidence;

public sealed class HttpEvidenceDashboardClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpEvidenceDashboardClient> logger) : IEvidenceDashboardClient
{
    public async Task<IReadOnlyList<EvidenceSummaryData>> ListClassEvidenceSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Evidence:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Evidence base URL is not configured.");
            throw new InvalidOperationException("Evidence service is not configured.");
        }

        var query = $"organisationId={organisationId}&classId={classId}";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/evidence/class-summary?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Evidence class summary request failed with status {StatusCode}.",
                response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<List<ClassEvidenceSummaryPayload>>(
            cancellationToken: cancellationToken);
        return payload?.Select(item => new EvidenceSummaryData(item.AssessmentId, item.ReviewedCount)).ToList()
            ?? [];
    }

    private sealed record ClassEvidenceSummaryPayload(Guid AssessmentId, int ReviewedCount);
}
