using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssessmentService.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WePlatform.Tenancy;

namespace AssessmentService.Infrastructure.Organisation;

public sealed class HttpClassAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<HttpClassAccessChecker> logger) : IClassAccessChecker
{
    public Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        CheckClassMembershipCachedAsync(
            AccessCacheKeys.TeacherClass(organisationId, teacherUserId, classId),
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
        CheckClassMembershipCachedAsync(
            AccessCacheKeys.StudentClass(organisationId, studentUserId, classId),
            studentUserId,
            organisationId,
            classId,
            bearerToken,
            (schoolClass, userId) => schoolClass.StudentUserIds?.Contains(userId) == true,
            cancellationToken);

    public async Task<IReadOnlyList<string>> GetClassStudentUserIdsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var schoolClass = await GetClassAsync(organisationId, classId, bearerToken, cancellationToken);
        return schoolClass?.StudentUserIds ?? [];
    }

    private async Task<bool> CheckClassMembershipCachedAsync(
        string cacheKey,
        string userId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        Func<ClassResponse, string, bool> isMember,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

        var schoolClass = await GetClassAsync(organisationId, classId, bearerToken, cancellationToken);
        var allowed = schoolClass is not null && isMember(schoolClass, userId);
        cache.Set(cacheKey, allowed, AccessCacheKeys.DefaultDuration);
        return allowed;
    }

    private async Task<ClassResponse?> GetClassAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Organisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Organisation base URL is not configured.");
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/organisations/{organisationId}/classes/{classId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ClassResponse>(cancellationToken);
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
