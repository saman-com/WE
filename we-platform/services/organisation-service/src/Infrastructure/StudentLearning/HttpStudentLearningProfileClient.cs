using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrganisationService.Application;

namespace OrganisationService.Infrastructure.StudentLearning;

public sealed class HttpStudentLearningProfileClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpStudentLearningProfileClient> logger) : Application.IStudentLearningProfileClient
{
    public async Task SyncEnrollmentAsync(
        string studentUserId,
        Application.StudentProfileEnrollmentSync enrollment,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = RequireBaseUrl();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/api/v1/students/{studentUserId}/profile/enrollments");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = JsonContent.Create(new
        {
            enrollment.OrganisationId,
            enrollment.ClassId,
            enrollment.ClassName,
            enrollment.ClassCode
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Student learning profile sync failed with status {StatusCode}.",
                response.StatusCode);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<StudentProfileSummaryData?> GetProfileSummaryAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var summaries = await GetProfileSummariesAsync([studentUserId], bearerToken, cancellationToken);
        return summaries.FirstOrDefault();
    }

    public async Task<IReadOnlyList<StudentProfileSummaryData>> GetProfileSummariesAsync(
        IReadOnlyList<string> studentUserIds,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = RequireBaseUrl();

        if (studentUserIds.Count == 0)
        {
            return [];
        }

        var query = string.Join(
            "&",
            studentUserIds.Select(id => $"studentUserIds={Uri.EscapeDataString(id)}"));
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/students/profiles/summaries?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Student learning profile summaries failed with status {StatusCode}.",
                response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<List<ProfileSummaryPayload>>(
            cancellationToken: cancellationToken);
        return payload?
            .Select(item => new StudentProfileSummaryData(
                item.StudentUserId,
                item.EvidenceCount,
                item.LatestActivityAt))
            .ToList()
            ?? [];
    }

    public async Task<StudentProfileData?> GetProfileAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = RequireBaseUrl();
        IReadOnlyList<ClassEnrollmentSummaryPayload>? enrollments = null;
        string? firstStudentUserId = null;

        var timeline = await PagedHttp.FetchAllAsync(
            async (cursor, ct) =>
            {
                var query = $"pageSize={PagedHttp.PageSize}";
                if (!string.IsNullOrWhiteSpace(cursor))
                {
                    query += $"&cursor={Uri.EscapeDataString(cursor)}";
                }

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{baseUrl.TrimEnd('/')}/api/v1/students/{studentUserId}/profile?{query}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                using var response = await httpClient.SendAsync(request, ct);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Student learning profile request failed with status {StatusCode}.",
                        response.StatusCode);
                    response.EnsureSuccessStatusCode();
                }

                var payload = await response.Content.ReadFromJsonAsync<ProfilePayload>(
                    cancellationToken: ct);
                if (payload is null)
                {
                    return null;
                }

                firstStudentUserId ??= payload.StudentUserId;
                enrollments ??= payload.Enrollments;

                return new PagedHttp.Page<StudentProfileTimelineEntryData>(
                    payload.EvidenceTimeline
                        .Select(entry => new StudentProfileTimelineEntryData(
                            entry.Id,
                            entry.Title,
                            entry.RecordedAt))
                        .ToList(),
                    payload.HasMore,
                    payload.NextCursor);
            },
            logger,
            "student-profile-timeline",
            cancellationToken);

        if (firstStudentUserId is null)
        {
            return null;
        }

        return new StudentProfileData(firstStudentUserId, timeline);
    }

    private string RequireBaseUrl()
    {
        var baseUrl = configuration["StudentLearning:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Student learning base URL is not configured.");
            throw new InvalidOperationException("Student learning service is not configured.");
        }

        return baseUrl;
    }

    private sealed record ProfileSummaryPayload(
        string StudentUserId,
        int EvidenceCount,
        DateTimeOffset? LatestActivityAt);

    private sealed record ProfilePayload(
        string StudentUserId,
        IReadOnlyList<ClassEnrollmentSummaryPayload> Enrollments,
        IReadOnlyList<ProfileTimelinePayload> EvidenceTimeline,
        bool HasMore = false,
        string? NextCursor = null);

    private sealed record ClassEnrollmentSummaryPayload(
        Guid OrganisationId,
        Guid ClassId,
        string ClassName,
        string ClassCode,
        DateTimeOffset EnrolledAt);

    private sealed record ProfileTimelinePayload(
        Guid Id,
        string Title,
        DateTimeOffset RecordedAt);
}
