namespace AiGatewayService.Domain;

public sealed class AiAuditLog
{
    public Guid Id { get; set; }
    public string CallerUserId { get; set; } = string.Empty;
    public string PromptId { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string ContextScope { get; set; } = string.Empty;
    public string PromptVariablesJson { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? BlockReason { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
