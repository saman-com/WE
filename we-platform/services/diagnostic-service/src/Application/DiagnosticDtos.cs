namespace DiagnosticService.Application;

public sealed record AnalyzedMicroSkillDiagnostic(
    Guid EvidenceId,
    Guid AssessmentId,
    Guid MicroSkillId,
    string Status,
    decimal Mark,
    string Reason);

public sealed record MicroSkillDiagnosticResponse(
    Guid Id,
    Guid EvidenceId,
    Guid AssessmentId,
    Guid MicroSkillId,
    string Status,
    decimal Mark,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record StudentDiagnosticsResponse(
    string StudentUserId,
    IReadOnlyList<MicroSkillDiagnosticResponse> Diagnostics);
