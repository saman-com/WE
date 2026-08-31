using InterventionService.Application;

namespace InterventionService.Tests;

public sealed class FakeOrganisationAccessChecker : IOrganisationAccessChecker
{
    private readonly HashSet<(string UserId, string StudentUserId)> _teacherPairs = [];
    private readonly HashSet<(string UserId, string StudentUserId)> _schoolLeaderPairs = [];
    private readonly HashSet<(string UserId, string StudentUserId)> _parentPairs = [];
    private readonly HashSet<(string UserId, Guid OrganisationId)> _schoolLeaderOrganisations = [];

    public void AllowTeacher(string teacherUserId, string studentUserId) =>
        _teacherPairs.Add((teacherUserId, studentUserId));

    public void AllowSchoolLeader(string schoolLeaderUserId, string studentUserId) =>
        _schoolLeaderPairs.Add((schoolLeaderUserId, studentUserId));

    public void AllowSchoolLeaderForOrganisation(string schoolLeaderUserId, Guid organisationId) =>
        _schoolLeaderOrganisations.Add((schoolLeaderUserId, organisationId));

    public void AllowParent(string parentUserId, string studentUserId) =>
        _parentPairs.Add((parentUserId, studentUserId));

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

    public Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_parentPairs.Contains((parentUserId, studentUserId)));

    public Task<bool> SchoolLeaderCanViewOrganisationAsync(
        string schoolLeaderUserId,
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_schoolLeaderOrganisations.Contains((schoolLeaderUserId, organisationId)));
}
