namespace AssessmentService.Application;

public record CreateAssessmentRequest(
    Guid OrganisationId,
    Guid ClassId,
    string Title,
    string? Instructions,
    DateTimeOffset? DueAt,
    IReadOnlyList<Guid> LearningObjectiveIds,
    IReadOnlyList<Guid> MicroSkillIds);

public record UpdateAssessmentRequest(
    string Title,
    string? Instructions,
    DateTimeOffset? DueAt,
    IReadOnlyList<Guid> LearningObjectiveIds,
    IReadOnlyList<Guid> MicroSkillIds);

public record AssessmentResponse(
    Guid Id,
    Guid OrganisationId,
    Guid ClassId,
    string Title,
    string? Instructions,
    DateTimeOffset? DueAt,
    string Status,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<Guid> LearningObjectiveIds,
    IReadOnlyList<Guid> MicroSkillIds,
    string CreatedByTeacherUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SubmitAssessmentRequest(string Responses);

public record SubmissionResponse(
    Guid Id,
    Guid AssessmentId,
    string StudentUserId,
    string Responses,
    string Status,
    bool IsLate,
    DateTimeOffset SubmittedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ClassAssessmentSummaryResponse(
    Guid Id,
    string Title,
    string Status,
    DateTimeOffset? DueAt,
    int SubmissionCount,
    int ReviewedCount);

public record StudentAssessmentSummaryResponse(
    Guid Id,
    string Title,
    DateTimeOffset? DueAt,
    IReadOnlyList<Guid> LearningObjectiveIds,
    bool HasSubmitted,
    DateTimeOffset? SubmittedAt);
