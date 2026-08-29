using DiagnosticService.Application;

namespace DiagnosticService.Tests;

public sealed class FakeOrganisationAccessChecker : IOrganisationAccessChecker
{
    private readonly HashSet<(string TeacherUserId, string StudentUserId)> _allowedPairs = [];

    public void Allow(string teacherUserId, string studentUserId) =>
        _allowedPairs.Add((teacherUserId, studentUserId));

    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_allowedPairs.Contains((teacherUserId, studentUserId)));
}
