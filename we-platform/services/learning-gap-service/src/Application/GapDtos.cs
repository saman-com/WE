namespace LearningGapService.Application;

public sealed record MicroSkillDiagnosticInput(
    Guid EvidenceId,
    Guid AssessmentId,
    Guid MicroSkillId,
    string Status,
    decimal Mark,
    string DiagnosticReason);

public sealed record CalculatedLearningGap(
    Guid EvidenceId,
    Guid AssessmentId,
    Guid MicroSkillId,
    string ExpectedMastery,
    string ActualMastery,
    decimal Mark,
    string Severity,
    string Urgency,
    string Explanation);

public sealed record LearningGapResponse(
    Guid Id,
    Guid EvidenceId,
    Guid AssessmentId,
    Guid MicroSkillId,
    Guid? LearningObjectiveId,
    string ExpectedMastery,
    string ActualMastery,
    decimal Mark,
    string Severity,
    string Urgency,
    string Explanation,
    DateTimeOffset CreatedAt);

public sealed record StudentLearningGapsResponse(
    string StudentUserId,
    IReadOnlyList<LearningGapResponse> Gaps);
