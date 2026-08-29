using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeStudentLearningProfileClient : IStudentLearningProfileClient
{
    public List<(string StudentUserId, StudentProfileEnrollmentSync Enrollment)> SyncCalls { get; } = [];
    public Dictionary<string, StudentProfileSummaryData> Summaries { get; } = [];

    public Task SyncEnrollmentAsync(
        string studentUserId,
        StudentProfileEnrollmentSync enrollment,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        SyncCalls.Add((studentUserId, enrollment));
        return Task.CompletedTask;
    }

    public Task<StudentProfileSummaryData?> GetProfileSummaryAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        Summaries.TryGetValue(studentUserId, out var summary);
        return Task.FromResult(summary);
    }
}
