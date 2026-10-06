using MasteryService.Application;

namespace MasteryService.Tests;

public sealed class FakeOrganisationAccessChecker : IOrganisationAccessChecker
{
    private readonly HashSet<(string TeacherUserId, string StudentUserId)> _allowedPairs = [];
    private readonly HashSet<(string ParentUserId, string StudentUserId)> _parentPairs = [];
    private readonly HashSet<(string LeaderUserId, string StudentUserId)> _leaderPairs = [];

    public void Allow(string teacherUserId, string studentUserId) =>
        _allowedPairs.Add((teacherUserId, studentUserId));

    public void AllowParent(string parentUserId, string studentUserId) =>
        _parentPairs.Add((parentUserId, studentUserId));

    public void AllowLeader(string schoolLeaderUserId, string studentUserId) =>
        _leaderPairs.Add((schoolLeaderUserId, studentUserId));

    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_allowedPairs.Contains((teacherUserId, studentUserId)));

    public Task<bool> SchoolLeaderCanViewStudentAsync(
        string schoolLeaderUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_leaderPairs.Contains((schoolLeaderUserId, studentUserId)));

    public Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_parentPairs.Contains((parentUserId, studentUserId)));
}
