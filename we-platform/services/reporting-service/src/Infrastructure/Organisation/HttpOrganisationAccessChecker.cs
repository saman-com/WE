using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportingService.Application;

namespace ReportingService.Infrastructure.Organisation;

public sealed class HttpOrganisationAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<HttpOrganisationAccessChecker> logger) : IOrganisationAccessChecker
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"access:teacher:{teacherUserId}:class:{organisationId}:{classId}";
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
            cache.Set(cacheKey, false, CacheDuration);
            return false;
        }

        var schoolClass = await response.Content.ReadFromJsonAsync<ClassResponse>(cancellationToken);
        var allowed = schoolClass?.TeacherUserIds?.Contains(teacherUserId) == true;
        cache.Set(cacheKey, allowed, CacheDuration);
        return allowed;
    }

    public async Task<bool> SchoolLeaderCanViewOrganisationAsync(
        string schoolLeaderUserId,
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"access:leader:{schoolLeaderUserId}:org:{organisationId}";
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
            $"{baseUrl.TrimEnd('/')}/api/v1/organisations/{organisationId}/leadership/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var allowed = response.IsSuccessStatusCode;
        cache.Set(cacheKey, allowed, CacheDuration);
        return allowed;
    }

    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        CheckAccessCachedAsync(
            $"access:teacher:{teacherUserId}:student:{studentUserId}",
            $"/api/v1/access/teacher/{Uri.EscapeDataString(teacherUserId)}/student/{Uri.EscapeDataString(studentUserId)}",
            bearerToken,
            cancellationToken);

    private async Task<bool> CheckAccessCachedAsync(
        string cacheKey,
        string path,
        string bearerToken,
        CancellationToken cancellationToken)
    {
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
            $"{baseUrl.TrimEnd('/')}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var allowed = response.IsSuccessStatusCode;
        cache.Set(cacheKey, allowed, CacheDuration);
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
