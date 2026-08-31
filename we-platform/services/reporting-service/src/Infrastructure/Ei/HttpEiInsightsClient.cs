using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportingService.Application;

namespace ReportingService.Infrastructure.Ei;

public sealed class HttpEiInsightsClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpEiInsightsClient> logger) : IEiInsightsClient
{
    public async Task<ClassEiInsightsData?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["Ei:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("EI base URL is not configured.");
            return null;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/ei/organisations/{organisationId}/classes/{classId}/insights");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("EI insights request failed with status {StatusCode}.", response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<ClassEiInsightsPayload>(cancellationToken);
        if (payload is null)
        {
            return null;
        }

        return new ClassEiInsightsData(
            payload.OrganisationId,
            payload.ClassId,
            payload.MasteryDistribution.Select(item => new ClassMasteryDistributionSection(
                item.MicroSkillId,
                item.LevelCounts,
                item.TotalStudents,
                item.Explanation,
                item.LinkedEvidenceIds)).ToList(),
            payload.ActiveLearningGaps.Select(item => new ClassActiveGapSection(
                item.GapId,
                item.MicroSkillId,
                item.Severity,
                item.Urgency,
                item.StudentUserId,
                item.Explanation,
                item.EvidenceId)).ToList());
    }

    private sealed record ClassEiInsightsPayload(
        Guid OrganisationId,
        Guid ClassId,
        IReadOnlyList<MasteryDistributionPayload> MasteryDistribution,
        IReadOnlyList<ActiveGapPayload> ActiveLearningGaps);

    private sealed record MasteryDistributionPayload(
        Guid MicroSkillId,
        IReadOnlyDictionary<string, int> LevelCounts,
        int TotalStudents,
        string Explanation,
        IReadOnlyList<Guid> LinkedEvidenceIds);

    private sealed record ActiveGapPayload(
        Guid GapId,
        Guid MicroSkillId,
        string Severity,
        string Urgency,
        string StudentUserId,
        string Explanation,
        Guid EvidenceId);
}
