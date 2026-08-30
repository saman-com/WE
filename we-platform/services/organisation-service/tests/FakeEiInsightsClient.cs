using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeEiInsightsClient : IEiInsightsClient
{
    public List<(Guid OrganisationId, Guid ClassId)> RequestedClasses { get; } = [];
    public Dictionary<(Guid OrganisationId, Guid ClassId), ClassEiInsightsData> InsightsByClass { get; } = [];

    public Task<ClassEiInsightsData?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        RequestedClasses.Add((organisationId, classId));
        if (InsightsByClass.TryGetValue((organisationId, classId), out var insights))
        {
            return Task.FromResult<ClassEiInsightsData?>(insights);
        }

        return Task.FromResult<ClassEiInsightsData?>(null);
    }
}
