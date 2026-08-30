namespace AssessmentService.Application;

public interface IClassAccessChecker
{
    Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<bool> StudentIsEnrolledInClassAsync(
        string studentUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetClassStudentUserIdsAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
