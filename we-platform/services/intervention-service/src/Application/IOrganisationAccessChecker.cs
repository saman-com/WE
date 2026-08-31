namespace InterventionService.Application;

public interface IOrganisationAccessChecker
{
    Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<bool> SchoolLeaderCanViewStudentAsync(
        string schoolLeaderUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<bool> SchoolLeaderCanViewOrganisationAsync(
        string schoolLeaderUserId,
        Guid organisationId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
