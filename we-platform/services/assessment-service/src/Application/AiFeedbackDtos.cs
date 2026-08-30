namespace AssessmentService.Application;

public record RequestAiFeedbackDraftRequest(Guid MicroSkillId);

public record AiFeedbackDraftResponse(
    Guid AuditLogId,
    string DraftFeedback,
    string PromptId,
    string PromptVersion);

public record FinalizeAiFeedbackAuditRequest(
    string TeacherEditedFeedback,
    Guid EvidenceId);

public record FinalizeAiFeedbackAuditResponse(
    Guid AuditLogId,
    DateTimeOffset FinalApprovedAt);

public record AiCompletionRequest(
    string PromptId,
    string PromptVersion,
    string ContextScope,
    IReadOnlyDictionary<string, string> Variables);

public record AiCompletionResult(
    string Content,
    string PromptId,
    string PromptVersion,
    string ProviderName,
    bool SafetyPassed);
