using EvidenceService.Application;

namespace EvidenceService.Tests;

public sealed class FakeClassAccessChecker : IClassAccessChecker
{
    private readonly HashSet<(string TeacherUserId, Guid OrganisationId, Guid ClassId)> _teacherClasses = [];

    public void AllowTeacher(string teacherUserId, Guid organisationId, Guid classId) =>
        _teacherClasses.Add((teacherUserId, organisationId, classId));

    public Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_teacherClasses.Contains((teacherUserId, organisationId, classId)));
}
