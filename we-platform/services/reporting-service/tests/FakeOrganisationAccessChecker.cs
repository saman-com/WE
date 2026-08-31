using ReportingService.Application;

namespace ReportingService.Tests;

public sealed class FakeOrganisationAccessChecker : IOrganisationAccessChecker
{
    private readonly HashSet<(string TeacherUserId, Guid OrganisationId, Guid ClassId)> _teacherClasses = [];
    private readonly HashSet<(string LeaderUserId, Guid OrganisationId)> _leaderOrganisations = [];

    public void AllowTeacher(string teacherUserId, Guid organisationId, Guid classId) =>
        _teacherClasses.Add((teacherUserId, organisationId, classId));

    public void AllowSchoolLeader(string leaderUserId, Guid organisationId) =>
        _leaderOrganisations.Add((leaderUserId, organisationId));

    public Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_teacherClasses.Contains((teacherUserId, organisationId, classId)));

    public Task<bool> SchoolLeaderCanViewOrganisationAsync(
        string schoolLeaderUserId,
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_leaderOrganisations.Contains((schoolLeaderUserId, organisationId)));
}
