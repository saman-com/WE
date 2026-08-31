using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportingService.Application;

namespace ReportingService.Infrastructure.Organisation;

public sealed class HttpSchoolSummaryClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpSchoolSummaryClient> logger) : ISchoolSummaryClient
{
    public async Task<SchoolSummaryData?> GetSchoolSummaryAsync(
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Organisation:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Organisation base URL is not configured.");
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/organisations/{organisationId}/leadership/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("School summary request failed with status {StatusCode}.", response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<LeadershipDashboardPayload>(cancellationToken);
        if (payload is null)
        {
            return null;
        }

        return new SchoolSummaryData(
            payload.OrganisationId,
            payload.OrganisationName,
            new LeadershipKpiSection(
                payload.Kpis.TotalStudents,
                payload.Kpis.TotalClasses,
                payload.Kpis.ActiveInterventions,
                payload.Kpis.ActiveLearningGaps,
                payload.Kpis.StudentsNeedingAttention,
                payload.Kpis.AssessmentCompletionRate,
                payload.Kpis.MasteryLevelCounts),
            payload.YearLevels.Select(item => new YearLevelSummarySection(
                item.YearLevelId,
                item.YearLevelName,
                item.ClassCount,
                item.StudentCount,
                item.ActiveInterventions,
                item.ActiveLearningGaps)).ToList(),
            payload.ClassComparisons.Select(item => new ClassComparisonSection(
                item.ClassId,
                item.ClassName,
                item.YearLevelId,
                item.YearLevelName,
                item.StudentCount,
                item.ActiveInterventions,
                item.ActiveLearningGaps,
                item.StudentsNeedingAttention,
                item.AssessmentCompletionRate)).ToList());
    }

    private sealed record LeadershipDashboardPayload(
        Guid OrganisationId,
        string OrganisationName,
        KpiPayload Kpis,
        IReadOnlyList<YearLevelPayload> YearLevels,
        IReadOnlyList<ClassComparisonPayload> ClassComparisons);

    private sealed record KpiPayload(
        int TotalStudents,
        int TotalClasses,
        int ActiveInterventions,
        int ActiveLearningGaps,
        int StudentsNeedingAttention,
        decimal AssessmentCompletionRate,
        IReadOnlyDictionary<string, int> MasteryLevelCounts);

    private sealed record YearLevelPayload(
        Guid YearLevelId,
        string YearLevelName,
        int ClassCount,
        int StudentCount,
        int ActiveInterventions,
        int ActiveLearningGaps);

    private sealed record ClassComparisonPayload(
        Guid ClassId,
        string ClassName,
        Guid YearLevelId,
        string YearLevelName,
        int StudentCount,
        int ActiveInterventions,
        int ActiveLearningGaps,
        int StudentsNeedingAttention,
        decimal AssessmentCompletionRate);
}
