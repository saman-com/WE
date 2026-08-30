using InterventionService.Application;

namespace InterventionService.Tests;

public sealed class FakeOrganisationAccessChecker : IOrganisationAccessChecker
{
    private readonly HashSet<(string UserId, string StudentUserId)> _teacherPairs = [];
    private readonly HashSet<(string UserId, string StudentUserId)> _schoolLeaderPairs = [];

    public void AllowTeacher(string teacherUserId, string studentUserId) =>
        _teacherPairs.Add((teacherUserId, studentUserId));

    public void AllowSchoolLeader(string schoolLeaderUserId, string studentUserId) =>
        _schoolLeaderPairs.Add((schoolLeaderUserId, studentUserId));

    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_teacherPairs.Contains((teacherUserId, studentUserId)));

    public Task<bool> SchoolLeaderCanViewStudentAsync(
        string schoolLeaderUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_schoolLeaderPairs.Contains((schoolLeaderUserId, studentUserId)));
}
