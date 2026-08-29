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
        var baseUrl = configuration["StudentLearning:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Student learning base URL is not configured.");
            throw new InvalidOperationException("Student learning service is not configured.");
        }

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
        var baseUrl = configuration["StudentLearning:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Student learning base URL is not configured.");
            throw new InvalidOperationException("Student learning service is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/students/{studentUserId}/profile/summary");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Student learning profile summary failed with status {StatusCode}.",
                response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<ProfileSummaryPayload>(
            cancellationToken: cancellationToken);
        return payload is null
            ? null
            : new StudentProfileSummaryData(
                payload.StudentUserId,
                payload.EvidenceCount,
                payload.LatestActivityAt);
    }

    public async Task<StudentProfileData?> GetProfileAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["StudentLearning:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Student learning base URL is not configured.");
            throw new InvalidOperationException("Student learning service is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/students/{studentUserId}/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
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
            cancellationToken: cancellationToken);
        return payload is null
            ? null
            : new StudentProfileData(
                payload.StudentUserId,
                payload.EvidenceTimeline
                    .Select(entry => new StudentProfileTimelineEntryData(
                        entry.Id,
                        entry.Title,
                        entry.RecordedAt))
                    .ToList());
    }

    private sealed record ProfileSummaryPayload(
        string StudentUserId,
        int EvidenceCount,
        DateTimeOffset? LatestActivityAt);

    private sealed record ProfilePayload(
        string StudentUserId,
        IReadOnlyList<ProfileTimelinePayload> EvidenceTimeline);

    private sealed record ProfileTimelinePayload(
        Guid Id,
        string Title,
        DateTimeOffset RecordedAt);
}
