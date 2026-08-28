namespace StudentLearningService.Application;

public interface IOrganisationAccessChecker
{
    Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
