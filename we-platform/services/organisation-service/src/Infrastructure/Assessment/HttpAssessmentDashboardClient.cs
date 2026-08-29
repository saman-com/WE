using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrganisationService.Application;

namespace OrganisationService.Infrastructure.Assessment;

public sealed class HttpAssessmentDashboardClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpAssessmentDashboardClient> logger) : IAssessmentDashboardClient
{
    public async Task<IReadOnlyList<AssessmentSummaryData>> ListClassAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Assessment:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Assessment base URL is not configured.");
            throw new InvalidOperationException("Assessment service is not configured.");
        }

        var query = $"organisationId={organisationId}&classId={classId}";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/assessments/class-summary?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Assessment class summary request failed with status {StatusCode}.",
                response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<List<ClassAssessmentSummaryPayload>>(
            cancellationToken: cancellationToken);
        return payload?.Select(item => new AssessmentSummaryData(
            item.Id,
            item.Title,
            item.Status,
            item.DueAt,
            item.SubmissionCount)).ToList()
            ?? [];
    }

    private sealed record ClassAssessmentSummaryPayload(
        Guid Id,
        string Title,
        string Status,
        DateTimeOffset? DueAt,
        int SubmissionCount,
        int ReviewedCount);
}
