using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportingService.Application;

namespace ReportingService.Infrastructure.Organisation;

public sealed class HttpClassDashboardClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpClassDashboardClient> logger) : IClassDashboardClient
{
    public async Task<ClassDashboardData?> GetClassDashboardAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Organisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Organisation base URL is not configured.");
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/organisations/{organisationId}/classes/{classId}/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Class dashboard request failed with status {StatusCode}.", response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<ClassDashboardPayload>(cancellationToken);
        if (payload is null)
        {
            return null;
        }

        return new ClassDashboardData(
            payload.Class.Id,
            payload.Class.Name,
            payload.RecentAssessments.Select(item => new AssessmentSummarySection(
                item.Id,
                item.Title,
                item.Status,
                item.SubmissionCount,
                item.ReviewedCount,
                item.DueAt)).ToList());
    }

    private sealed record ClassDashboardPayload(
        ClassPayload Class,
        IReadOnlyList<AssessmentPayload> RecentAssessments);

    private sealed record ClassPayload(Guid Id, string Name);

    private sealed record AssessmentPayload(
        Guid Id,
        string Title,
        string Status,
        int SubmissionCount,
        int ReviewedCount,
        DateTimeOffset? DueAt);
}
