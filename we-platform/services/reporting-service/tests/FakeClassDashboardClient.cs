using ReportingService.Application;

namespace ReportingService.Tests;

public sealed class FakeClassDashboardClient : IClassDashboardClient
{
    private readonly Dictionary<(Guid OrganisationId, Guid ClassId), ClassDashboardData> _responses = new();

    public void SetResponse(Guid organisationId, Guid classId, ClassDashboardData data) =>
        _responses[(organisationId, classId)] = data;

    public Task<ClassDashboardData?> GetClassDashboardAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        _responses.TryGetValue((organisationId, classId), out var data);
        return Task.FromResult(data);
    }
}
