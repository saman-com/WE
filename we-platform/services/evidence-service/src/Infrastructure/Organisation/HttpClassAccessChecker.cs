using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EvidenceService.Application;

namespace EvidenceService.Infrastructure.Organisation;

public sealed class HttpClassAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpClassAccessChecker> logger) : IClassAccessChecker
{
    public async Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
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
        return schoolClass is not null && schoolClass.TeacherUserIds?.Contains(teacherUserId) == true;
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
