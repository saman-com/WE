using MasteryService.Application;

namespace MasteryService.Tests;

public sealed class FakeOrganisationAccessChecker : IOrganisationAccessChecker
{
    private readonly HashSet<(string TeacherUserId, string StudentUserId)> _allowedPairs = [];
    private readonly HashSet<(string ParentUserId, string StudentUserId)> _parentPairs = [];

    public void Allow(string teacherUserId, string studentUserId) =>
        _allowedPairs.Add((teacherUserId, studentUserId));

    public void AllowParent(string parentUserId, string studentUserId) =>
        _parentPairs.Add((parentUserId, studentUserId));

    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_allowedPairs.Contains((teacherUserId, studentUserId)));

    public Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_parentPairs.Contains((parentUserId, studentUserId)));
}
