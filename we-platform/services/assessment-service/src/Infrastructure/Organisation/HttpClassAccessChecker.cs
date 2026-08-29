using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AssessmentService.Application;

namespace AssessmentService.Infrastructure.Organisation;

public sealed class HttpClassAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpClassAccessChecker> logger) : IClassAccessChecker
{
    public Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        CheckClassMembershipAsync(
            teacherUserId,
            organisationId,
            classId,
            bearerToken,
            (schoolClass, userId) => schoolClass.TeacherUserIds?.Contains(userId) == true,
            cancellationToken);

    public Task<bool> StudentIsEnrolledInClassAsync(
        string studentUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        CheckClassMembershipAsync(
            studentUserId,
            organisationId,
            classId,
            bearerToken,
            (schoolClass, userId) => schoolClass.StudentUserIds?.Contains(userId) == true,
            cancellationToken);

    private async Task<bool> CheckClassMembershipAsync(
        string userId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        Func<ClassResponse, string, bool> isMember,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Organisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Organisation base URL is not configured.");
            return false;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/organisations/{organisationId}/classes/{classId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var schoolClass = await response.Content.ReadFromJsonAsync<ClassResponse>(cancellationToken);
        return schoolClass is not null && isMember(schoolClass, userId);
    }

    private sealed record ClassResponse(
        Guid Id,
        Guid OrganisationId,
        Guid YearLevelId,
        string Name,
        string Code,
        IReadOnlyList<string>? TeacherUserIds,
        IReadOnlyList<string>? StudentUserIds);
}
