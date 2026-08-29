using AssessmentService.Application;

namespace AssessmentService.Tests;

public sealed class FakeClassAccessChecker : IClassAccessChecker
{
    private readonly HashSet<(string TeacherUserId, Guid OrganisationId, Guid ClassId)> _teacherClasses = [];
    private readonly HashSet<(string StudentUserId, Guid OrganisationId, Guid ClassId)> _studentClasses = [];

    public void AllowTeacher(string teacherUserId, Guid organisationId, Guid classId) =>
        _teacherClasses.Add((teacherUserId, organisationId, classId));

    public void AllowStudent(string studentUserId, Guid organisationId, Guid classId) =>
        _studentClasses.Add((studentUserId, organisationId, classId));

    public Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_teacherClasses.Contains((teacherUserId, organisationId, classId)));

    public Task<bool> StudentIsEnrolledInClassAsync(
        string studentUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_studentClasses.Contains((studentUserId, organisationId, classId)));
}
