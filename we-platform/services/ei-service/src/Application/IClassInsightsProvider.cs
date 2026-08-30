namespace EiService.Application;

public interface IClassAccessChecker
{
    Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<bool> SchoolLeaderCanViewClassAsync(
        string schoolLeaderUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public interface IClassInsightsProvider
{
    Task<ClassEiInsightsResponse?> GetClassInsightsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
