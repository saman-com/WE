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
    public async Task<IReadOnlyList<AssessmentSummaryData>> GetRecentClassAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = RequireBaseUrl();
        // class-summary is intentionally recent-only (server Take); request an explicit page size.
        var query = $"organisationId={organisationId}&classId={classId}&pageSize=10";
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

    public Task<IReadOnlyList<StudentAssessmentSummaryData>> ListStudentAssessmentSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        ListAllStudentSummariesAsync(
            $"organisationId={organisationId}&classId={classId}",
            bearerToken,
            "student-summary",
            cancellationToken);

    public Task<IReadOnlyList<StudentAssessmentSummaryData>> ListStudentAssessmentSummariesForStudentAsync(
        Guid organisationId,
        Guid classId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        ListAllStudentSummariesAsync(
            $"organisationId={organisationId}&classId={classId}&studentUserId={Uri.EscapeDataString(studentUserId)}",
            bearerToken,
            "student-summary(parent)",
            cancellationToken);

    private async Task<IReadOnlyList<StudentAssessmentSummaryData>> ListAllStudentSummariesAsync(
        string baseQuery,
        string bearerToken,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var baseUrl = RequireBaseUrl();

        return await PagedHttp.FetchAllAsync(
            async (cursor, ct) =>
            {
                var query = $"{baseQuery}&pageSize={PagedHttp.PageSize}";
                if (!string.IsNullOrWhiteSpace(cursor))
                {
                    query += $"&cursor={Uri.EscapeDataString(cursor)}";
                }

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{baseUrl.TrimEnd('/')}/api/v1/assessments/student-summary?{query}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                using var response = await httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Assessment student summary request failed with status {StatusCode}.",
                        response.StatusCode);
                    response.EnsureSuccessStatusCode();
                }

                var page = await PagedHttp.ReadPageAsync<StudentAssessmentSummaryPayload>(response, ct);
                if (page is null)
                {
                    return null;
                }

                return new PagedHttp.Page<StudentAssessmentSummaryData>(
                    page.Items.Select(item => new StudentAssessmentSummaryData(
                        item.Id,
                        item.Title,
                        item.DueAt,
                        item.LearningObjectiveIds,
                        item.HasSubmitted,
                        item.SubmittedAt)).ToList(),
                    page.HasMore,
                    page.NextCursor);
            },
            logger,
            resourceName,
            cancellationToken);
    }

    private string RequireBaseUrl()
    {
        var baseUrl = configuration["Assessment:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Assessment base URL is not configured.");
            throw new InvalidOperationException("Assessment service is not configured.");
        }

        return baseUrl;
    }

    private sealed record ClassAssessmentSummaryPayload(
        Guid Id,
        string Title,
        string Status,
        DateTimeOffset? DueAt,
        int SubmissionCount,
        int ReviewedCount);

    private sealed record StudentAssessmentSummaryPayload(
        Guid Id,
        string Title,
        DateTimeOffset? DueAt,
        IReadOnlyList<Guid> LearningObjectiveIds,
        bool HasSubmitted,
        DateTimeOffset? SubmittedAt);
}
