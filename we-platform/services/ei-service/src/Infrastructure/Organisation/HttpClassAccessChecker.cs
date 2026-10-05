using System.Net.Http.Headers;
using System.Net.Http.Json;
using EiService.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WePlatform.Tenancy;

namespace EiService.Infrastructure.Organisation;

public sealed class HttpClassAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<HttpClassAccessChecker> logger) : IClassAccessChecker
{
    public async Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = AccessCacheKeys.TeacherClass(organisationId, teacherUserId, classId);
        if (cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

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
            cache.Set(cacheKey, false, AccessCacheKeys.DefaultDuration);
            return false;
        }

        var schoolClass = await response.Content.ReadFromJsonAsync<ClassResponse>(cancellationToken);
        var allowed = schoolClass?.TeacherUserIds?.Contains(teacherUserId) == true;
        cache.Set(cacheKey, allowed, AccessCacheKeys.DefaultDuration);
        return allowed;
    }

    public async Task<bool> SchoolLeaderCanViewClassAsync(
        string schoolLeaderUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = AccessCacheKeys.LeaderClass(organisationId, schoolLeaderUserId, classId);
        if (cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

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
        var allowed = response.IsSuccessStatusCode;
        cache.Set(cacheKey, allowed, AccessCacheKeys.DefaultDuration);
        return allowed;
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
