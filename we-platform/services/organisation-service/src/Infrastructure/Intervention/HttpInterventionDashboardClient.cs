using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrganisationService.Application;

namespace OrganisationService.Infrastructure.Intervention;

public sealed class HttpInterventionDashboardClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpInterventionDashboardClient> logger) : IInterventionDashboardClient
{
    public async Task<IReadOnlyList<ParentInterventionSummaryData>> ListActiveInterventionsAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Intervention:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Intervention base URL is not configured.");
            return [];
        }

        var query = $"studentUserId={Uri.EscapeDataString(studentUserId)}";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/interventions/parent-summary?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Intervention parent summary request failed with status {StatusCode}.",
                response.StatusCode);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<ParentInterventionsResponse>(cancellationToken);
        return payload?.Interventions
            .Select(item => new ParentInterventionSummaryData(
                item.Id,
                item.Summary,
                item.Status,
                item.PlannedStartAt,
                item.PlannedEndAt))
            .ToList()
            ?? [];
    }

    public async Task<IReadOnlyList<InterventionDetailData>> ListOrganisationInterventionsAsync(
        Guid organisationId,
        string? status,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Intervention:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Intervention base URL is not configured.");
            return [];
        }

        var query = string.IsNullOrWhiteSpace(status)
            ? string.Empty
            : $"?status={Uri.EscapeDataString(status)}";
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/organisations/{organisationId}/interventions{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Organisation interventions request failed with status {StatusCode}.",
                response.StatusCode);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<OrganisationInterventionsResponse>(cancellationToken);
        return payload?.Interventions
            .Select(item => new InterventionDetailData(
                item.Id,
                item.OrganisationId,
                item.StudentUserId,
                item.LearningGapId,
                item.AssignedTeacherUserId,
                item.PlannedActions,
                item.Outcome,
                item.Status,
                item.PlannedStartAt,
                item.PlannedEndAt,
                item.ReviewAt,
                item.CreatedAt,
                item.UpdatedAt))
            .ToList()
            ?? [];
    }

    private sealed record InterventionDetailResponse(
        Guid Id,
        Guid OrganisationId,
        string StudentUserId,
        Guid LearningGapId,
        string AssignedTeacherUserId,
        string PlannedActions,
        string? Outcome,
        string Status,
        DateTimeOffset? PlannedStartAt,
        DateTimeOffset? PlannedEndAt,
        DateTimeOffset? ReviewAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record OrganisationInterventionsResponse(
        Guid OrganisationId,
        IReadOnlyList<InterventionDetailResponse> Interventions);

    private sealed record ParentInterventionSummaryResponse(
        Guid Id,
        string Summary,
        string Status,
        DateTimeOffset? PlannedStartAt,
        DateTimeOffset? PlannedEndAt);

    private sealed record ParentInterventionsResponse(
        string StudentUserId,
        IReadOnlyList<ParentInterventionSummaryResponse> Interventions);
}
