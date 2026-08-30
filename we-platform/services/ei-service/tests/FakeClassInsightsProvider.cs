using EiService.Application;

namespace EiService.Tests;

public sealed class FakeClassInsightsProvider : IClassInsightsProvider
{
    private readonly Dictionary<(Guid OrganisationId, Guid ClassId), ClassEiInsightsResponse> _responses = [];

    public void SetResponse(Guid organisationId, Guid classId, ClassEiInsightsResponse response) =>
        _responses[(organisationId, classId)] = response;

    public Task<ClassEiInsightsResponse?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        _responses.TryGetValue((organisationId, classId), out var response);
        return Task.FromResult(response);
    }
}
