using EiService.Application;

namespace EiService.Tests;

public sealed class FakeStudentEiDataClient : IStudentEiDataClient
{
    private readonly Dictionary<string, StudentEiSnapshot> _snapshots = new(StringComparer.Ordinal);

    public void SetSnapshot(string studentUserId, StudentEiSnapshot snapshot) =>
        _snapshots[studentUserId] = snapshot;

    public Task<StudentEiSnapshot?> GetStudentSnapshotAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshots.GetValueOrDefault(studentUserId));
}
