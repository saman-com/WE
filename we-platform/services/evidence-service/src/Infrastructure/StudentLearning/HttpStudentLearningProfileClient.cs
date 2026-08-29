using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EvidenceService.Application;

namespace EvidenceService.Infrastructure.StudentLearning;

public sealed class HttpStudentLearningProfileClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpStudentLearningProfileClient> logger) : IStudentLearningProfileClient
{
    public async Task RecordEvidenceAsync(
        string studentUserId,
        Guid evidenceId,
        Guid assessmentId,
        IReadOnlyList<Guid> microSkillIds,
        string title,
        DateTimeOffset recordedAt,
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
            $"{baseUrl.TrimEnd('/')}/api/v1/students/{studentUserId}/profile/evidence");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Content = JsonContent.Create(new
        {
            EvidenceId = evidenceId,
            AssessmentId = assessmentId,
            MicroSkillIds = microSkillIds,
            Title = title,
            RecordedAt = recordedAt
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Student learning evidence sync failed with status {StatusCode}.",
                response.StatusCode);
            response.EnsureSuccessStatusCode();
        }
    }
}
