using CommunicationService.Application;

namespace CommunicationService.Tests;

public sealed class FakeOrganisationAccessChecker : IOrganisationAccessChecker
{
    private readonly HashSet<(string UserId, string StudentUserId)> _teacherPairs = [];
    private readonly HashSet<(string UserId, string StudentUserId)> _parentPairs = [];

    public void AllowTeacher(string teacherUserId, string studentUserId) =>
        _teacherPairs.Add((teacherUserId, studentUserId));

    public void AllowParent(string parentUserId, string studentUserId) =>
        _parentPairs.Add((parentUserId, studentUserId));

    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_teacherPairs.Contains((teacherUserId, studentUserId)));

    public Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_parentPairs.Contains((parentUserId, studentUserId)));
}
