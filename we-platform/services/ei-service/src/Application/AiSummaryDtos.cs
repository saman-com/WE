namespace EiService.Application;

public record RequestLessonSummaryDraftRequest(Guid UnitId);

public record RequestProgressReportDraftRequest(Guid OrganisationId, Guid ClassId);

public record AiSummaryDraftResponse(
    Guid AuditLogId,
    string DraftContent,
    string PromptId,
    string PromptVersion,
    bool IsAiAssistedDraft);

public record FinalizeAiSummaryAuditRequest(string TeacherEditedContent);

public record FinalizeAiSummaryAuditResponse(
    Guid AuditLogId,
    DateTimeOffset FinalApprovedAt,
    bool IsAiAssistedDraft);

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
