using ReportingService.Application;

namespace ReportingService.Tests;

public sealed class FakeEiInsightsClient : IEiInsightsClient
{
    private readonly Dictionary<(Guid OrganisationId, Guid ClassId), ClassEiInsightsData> _responses = new();

    public void SetResponse(Guid organisationId, Guid classId, ClassEiInsightsData data) =>
        _responses[(organisationId, classId)] = data;

    public Task<ClassEiInsightsData?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        _responses.TryGetValue((organisationId, classId), out var data);
        return Task.FromResult(data);
    }
}
