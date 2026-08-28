using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
}
