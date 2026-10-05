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
        var baseUrl = RequireBaseUrl();
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

    public Task<IReadOnlyList<StudentEvidenceFeedbackData>> ListStudentFeedbackAsync(
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        ListAllStudentFeedbackAsync(null, bearerToken, "student-feedback", cancellationToken);

    public Task<IReadOnlyList<StudentEvidenceFeedbackData>> ListStudentFeedbackForStudentAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        ListAllStudentFeedbackAsync(studentUserId, bearerToken, "student-feedback(parent)", cancellationToken);

    private async Task<IReadOnlyList<StudentEvidenceFeedbackData>> ListAllStudentFeedbackAsync(
        string? studentUserId,
        string bearerToken,
        string resourceName,
        CancellationToken cancellationToken)
    {
        var baseUrl = RequireBaseUrl();

        return await PagedHttp.FetchAllAsync(
            async (cursor, ct) =>
            {
                var queryParts = new List<string> { $"pageSize={PagedHttp.PageSize}" };
                if (!string.IsNullOrWhiteSpace(studentUserId))
                {
                    queryParts.Add($"studentUserId={Uri.EscapeDataString(studentUserId)}");
                }

                if (!string.IsNullOrWhiteSpace(cursor))
                {
                    queryParts.Add($"cursor={Uri.EscapeDataString(cursor)}");
                }

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{baseUrl.TrimEnd('/')}/api/v1/evidence/student-feedback?{string.Join("&", queryParts)}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                using var response = await httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Evidence student feedback request failed with status {StatusCode}.",
                        response.StatusCode);
                    response.EnsureSuccessStatusCode();
                }

                var page = await PagedHttp.ReadPageAsync<StudentFeedbackPayload>(response, ct);
                if (page is null)
                {
                    return null;
                }

                return new PagedHttp.Page<StudentEvidenceFeedbackData>(
                    MapFeedback(page.Items),
                    page.HasMore,
                    page.NextCursor);
            },
            logger,
            resourceName,
            cancellationToken);
    }

    private string RequireBaseUrl()
    {
        var baseUrl = configuration["Evidence:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Evidence base URL is not configured.");
            throw new InvalidOperationException("Evidence service is not configured.");
        }

        return baseUrl;
    }

    private static IReadOnlyList<StudentEvidenceFeedbackData> MapFeedback(
        IReadOnlyList<StudentFeedbackPayload> payload) =>
        payload.Select(item => new StudentEvidenceFeedbackData(
            item.Id,
            item.AssessmentId,
            item.Title,
            item.ApprovedAt,
            item.MicroSkillMarks
                .Select(mark => new StudentEvidenceFeedbackMarkData(
                    mark.MicroSkillId,
                    mark.Mark,
                    mark.Feedback))
                .ToList())).ToList();

    private sealed record ClassEvidenceSummaryPayload(Guid AssessmentId, int ReviewedCount);

    private sealed record StudentFeedbackPayload(
        Guid Id,
        Guid AssessmentId,
        string Title,
        DateTimeOffset ApprovedAt,
        IReadOnlyList<StudentFeedbackMarkPayload> MicroSkillMarks);

    private sealed record StudentFeedbackMarkPayload(
        Guid MicroSkillId,
        decimal Mark,
        string Feedback);
}
