using AssessmentService.Application;

namespace AssessmentService.Tests;

public sealed class FakeParentAccessChecker : IParentAccessChecker
{
    private readonly HashSet<(string ParentUserId, string StudentUserId)> _allowedPairs = [];

    public void Allow(string parentUserId, string studentUserId) =>
        _allowedPairs.Add((parentUserId, studentUserId));

    public Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_allowedPairs.Contains((parentUserId, studentUserId)));
}
