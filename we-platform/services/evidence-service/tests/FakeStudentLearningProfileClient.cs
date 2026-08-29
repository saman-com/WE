using EvidenceService.Application;

namespace EvidenceService.Tests;

public sealed class FakeStudentLearningProfileClient : IStudentLearningProfileClient
{
    public List<RecordedEvidence> Recorded { get; } = [];

    public Task RecordEvidenceAsync(
        string studentUserId,
        Guid evidenceId,
        Guid assessmentId,
        IReadOnlyList<Guid> microSkillIds,
        string title,
        DateTimeOffset recordedAt,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        Recorded.Add(new RecordedEvidence(
            studentUserId,
            evidenceId,
            assessmentId,
            microSkillIds.ToList(),
            title,
            recordedAt));
        return Task.CompletedTask;
    }

    public sealed record RecordedEvidence(
        string StudentUserId,
        Guid EvidenceId,
        Guid AssessmentId,
        IReadOnlyList<Guid> MicroSkillIds,
        string Title,
        DateTimeOffset RecordedAt);
}
