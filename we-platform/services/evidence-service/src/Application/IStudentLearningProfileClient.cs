namespace EvidenceService.Application;

public interface IStudentLearningProfileClient
{
    Task RecordEvidenceAsync(
        string studentUserId,
        Guid evidenceId,
        Guid assessmentId,
        IReadOnlyList<Guid> microSkillIds,
        string title,
        DateTimeOffset recordedAt,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
