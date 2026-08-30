using WePlatform.Events;

namespace MasteryService.Application;

public interface IMasteryProcessor
{
    Task ProcessEvidenceCreatedAsync(EvidenceCreated evidence, CancellationToken cancellationToken = default);
}

public interface IOrganisationAccessChecker
{
    Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
