using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class FakeStudentLearningProfileClient : IStudentLearningProfileClient
{
    public List<(string StudentUserId, StudentProfileEnrollmentSync Enrollment)> SyncCalls { get; } = [];

    public Task SyncEnrollmentAsync(
        string studentUserId,
        StudentProfileEnrollmentSync enrollment,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        SyncCalls.Add((studentUserId, enrollment));
        return Task.CompletedTask;
    }
}
