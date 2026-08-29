namespace EvidenceService.Application;

public record MicroSkillMarkRequest(Guid MicroSkillId, decimal Mark, string Feedback);

public record ApproveEvidenceRequest(
    Guid OrganisationId,
    Guid ClassId,
    Guid AssessmentId,
    Guid SubmissionId,
    string StudentUserId,
    string Title,
    IReadOnlyList<MicroSkillMarkRequest> MicroSkillMarks);

public record MicroSkillMarkResponse(Guid MicroSkillId, decimal Mark, string Feedback);

public record EvidenceResponse(
    Guid Id,
    Guid AssessmentId,
    Guid SubmissionId,
    string StudentUserId,
    string Title,
    string Status,
    IReadOnlyList<MicroSkillMarkResponse> MicroSkillMarks,
    string ApprovedByTeacherUserId,
    DateTimeOffset ApprovedAt);
