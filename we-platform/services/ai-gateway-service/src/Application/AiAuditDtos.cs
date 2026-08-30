namespace AiGatewayService.Application;

public sealed record AiAuditLogResponse(
    Guid Id,
    string CallerUserId,
    string PromptId,
    string PromptVersion,
    string ContextScope,
    string Outcome,
    string? BlockReason,
    string ProviderName,
    DateTimeOffset CreatedAt);

public sealed record AiAuditLogQuery(
    string? PromptId,
    string? Outcome,
    string? CallerUserId,
    string? Search,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Limit = 100);

public sealed record AiCompletionErrorResponse(string Reason);

public sealed class AiCompletionBlockedException(string reason) : Exception(reason)
{
    public string Reason { get; } = reason;
}
