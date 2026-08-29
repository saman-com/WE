using System.Net.Http.Headers;
using System.Net.Http.Json;
using DiagnosticService.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiagnosticService.Infrastructure.Organisation;

public sealed class HttpOrganisationAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpOrganisationAccessChecker> logger) : IOrganisationAccessChecker
{
    public async Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Organisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Organisation base URL is not configured.");
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/api/v1/organisations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var organisations = await response.Content.ReadFromJsonAsync<List<OrganisationResponse>>(cancellationToken);
        if (organisations is null)
        {
            return false;
        }

        foreach (var organisation in organisations)
        {
            using var classRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl.TrimEnd('/')}/api/v1/organisations/{organisation.Id}/classes");
            classRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var classResponse = await httpClient.SendAsync(classRequest, cancellationToken);
            if (!classResponse.IsSuccessStatusCode)
            {
                continue;
            }

            var classes = await classResponse.Content.ReadFromJsonAsync<List<ClassResponse>>(cancellationToken);
            if (classes is null)
            {
                continue;
            }

            foreach (var schoolClass in classes)
            {
                var teachesClass = schoolClass.TeacherUserIds?.Contains(teacherUserId) == true;
                var hasStudent = schoolClass.StudentUserIds?.Contains(studentUserId) == true;
                if (teachesClass && hasStudent)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private sealed record OrganisationResponse(Guid Id, string Name, string Code);

    private sealed record ClassResponse(
        Guid Id,
        Guid OrganisationId,
        Guid YearLevelId,
        string Name,
        string Code,
        IReadOnlyList<string>? TeacherUserIds,
        IReadOnlyList<string>? StudentUserIds);
}
