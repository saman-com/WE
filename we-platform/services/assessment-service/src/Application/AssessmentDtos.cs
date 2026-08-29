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
