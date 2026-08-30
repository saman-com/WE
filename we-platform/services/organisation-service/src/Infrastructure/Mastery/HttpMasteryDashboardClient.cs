using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrganisationService.Application;

namespace OrganisationService.Infrastructure.Mastery;

public sealed class HttpMasteryDashboardClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpMasteryDashboardClient> logger) : IMasteryDashboardClient
{
    public async Task<IReadOnlyList<ParentMasterySummaryData>> ListStudentMasteryAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Mastery:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Mastery base URL is not configured.");
            return [];
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/mastery/students/{Uri.EscapeDataString(studentUserId)}/parent-summary");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Mastery parent summary request failed with status {StatusCode}.",
                response.StatusCode);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<ParentMasteryResponse>(cancellationToken);
        return payload?.Records
            .Select(record => new ParentMasterySummaryData(record.MicroSkillId, record.MasteryLevel))
            .ToList()
            ?? [];
    }

    private sealed record ParentMasteryRecordResponse(Guid MicroSkillId, string MasteryLevel);

    private sealed record ParentMasteryResponse(
        string StudentUserId,
        IReadOnlyList<ParentMasteryRecordResponse> Records);
}
