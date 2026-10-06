using System.Net.Http.Headers;
using DiagnosticService.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WePlatform.Tenancy;

namespace DiagnosticService.Infrastructure.Organisation;

public sealed class HttpOrganisationAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache cache,
    ITenantContext tenantContext,
    ILogger<HttpOrganisationAccessChecker> logger) : IOrganisationAccessChecker
{
    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (!tenantContext.HasTenant)
        {
            return Task.FromResult(false);
        }

        return CheckAccessCachedAsync(
            AccessCacheKeys.TeacherStudent(tenantContext.TenantId!.Value, teacherUserId, studentUserId),
            $"/api/v1/access/teacher/{Uri.EscapeDataString(teacherUserId)}/student/{Uri.EscapeDataString(studentUserId)}",
            bearerToken,
            cancellationToken);
    }

    public Task<bool> SchoolLeaderCanViewStudentAsync(
        string schoolLeaderUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (!tenantContext.HasTenant)
        {
            return Task.FromResult(false);
        }

        return CheckAccessCachedAsync(
            AccessCacheKeys.LeaderStudent(tenantContext.TenantId!.Value, schoolLeaderUserId, studentUserId),
            $"/api/v1/access/school-leader/{Uri.EscapeDataString(schoolLeaderUserId)}/student/{Uri.EscapeDataString(studentUserId)}",
            bearerToken,
            cancellationToken);
    }

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

        var allowed = await CheckAccessAsync(path, bearerToken, cancellationToken);
        cache.Set(cacheKey, allowed, AccessCacheKeys.DefaultDuration);
        return allowed;
    }

    private async Task<bool> CheckAccessAsync(
        string path,
        string bearerToken,
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
            $"{baseUrl.TrimEnd('/')}{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
