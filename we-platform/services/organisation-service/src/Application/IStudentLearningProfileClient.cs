namespace OrganisationService.Application;

public sealed record StudentProfileEnrollmentSync(
    Guid OrganisationId,
    Guid ClassId,
    string ClassName,
    string ClassCode);

public interface IStudentLearningProfileClient
{
    Task SyncEnrollmentAsync(
        string studentUserId,
        StudentProfileEnrollmentSync enrollment,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
