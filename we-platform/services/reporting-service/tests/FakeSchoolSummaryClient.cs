using ReportingService.Application;

namespace ReportingService.Tests;

public sealed class FakeSchoolSummaryClient : ISchoolSummaryClient
{
    private readonly Dictionary<Guid, SchoolSummaryData> _responses = new();

    public void SetResponse(Guid organisationId, SchoolSummaryData data) =>
        _responses[organisationId] = data;

    public Task<SchoolSummaryData?> GetSchoolSummaryAsync(
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        _responses.TryGetValue(organisationId, out var data);
        return Task.FromResult(data);
    }
}
