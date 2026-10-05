using System.Net.Http.Headers;
using LearningGapService.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LearningGapService.Infrastructure.Organisation;

public sealed class HttpOrganisationAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<HttpOrganisationAccessChecker> logger) : IOrganisationAccessChecker
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

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

        var allowed = await CheckAccessAsync(path, bearerToken, cancellationToken);
        cache.Set(cacheKey, allowed, CacheDuration);
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
