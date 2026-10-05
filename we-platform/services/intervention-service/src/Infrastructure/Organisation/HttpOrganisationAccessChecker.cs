using System.Net.Http.Headers;
using System.Net.Http.Json;
using InterventionService.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InterventionService.Infrastructure.Organisation;

public sealed class HttpOrganisationAccessChecker(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpOrganisationAccessChecker> logger) : IOrganisationAccessChecker
{
    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        CheckTeacherAccessAsync(teacherUserId, studentUserId, bearerToken, cancellationToken);

    public Task<bool> SchoolLeaderCanViewStudentAsync(
        string schoolLeaderUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        UserCanViewStudentAsync(
            schoolLeaderUserId,
            studentUserId,
            bearerToken,
            requireTeacherAssignment: false,
            cancellationToken);

    public Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        CheckParentAccessAsync(parentUserId, studentUserId, bearerToken, cancellationToken);

    public async Task<bool> SchoolLeaderCanViewOrganisationAsync(
        string schoolLeaderUserId,
        Guid organisationId,
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
        return organisations?.Any(org => org.Id == organisationId) == true;
    }

    private async Task<bool> CheckTeacherAccessAsync(
        string teacherUserId,
        string studentUserId,
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
            $"{baseUrl.TrimEnd('/')}/api/v1/access/teacher/{Uri.EscapeDataString(teacherUserId)}/student/{Uri.EscapeDataString(studentUserId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> CheckParentAccessAsync(
        string parentUserId,
        string studentUserId,
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
            $"{baseUrl.TrimEnd('/')}/api/v1/access/parent/{Uri.EscapeDataString(parentUserId)}/student/{Uri.EscapeDataString(studentUserId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> UserCanViewStudentAsync(
        string userId,
        string studentUserId,
        string bearerToken,
        bool requireTeacherAssignment,
        CancellationToken cancellationToken)
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
                var hasStudent = schoolClass.StudentUserIds?.Contains(studentUserId) == true;
                if (!hasStudent)
                {
                    continue;
                }

                if (!requireTeacherAssignment)
                {
                    return true;
                }

                var teachesClass = schoolClass.TeacherUserIds?.Contains(userId) == true;
                if (teachesClass)
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
