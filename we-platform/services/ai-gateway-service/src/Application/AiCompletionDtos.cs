namespace AiGatewayService.Application;

public sealed record AiCompletionRequest(
    string PromptId,
    string PromptVersion,
    string ContextScope,
    IReadOnlyDictionary<string, string> Variables);

public sealed record AiCompletionResponse(
    string Content,
    string PromptId,
    string PromptVersion,
    string ProviderName,
    bool SafetyPassed);

public sealed record AiContext(
    string Scope,
    IReadOnlyDictionary<string, string> Items);

public sealed record AiProviderRequest(
    string RenderedPrompt,
    AiContext Context);

public sealed record AiProviderResponse(
    string Content,
    string ProviderName);

public sealed record ResponseValidationResult(
    bool IsValid,
    string? Reason);

public sealed record SafetyFilterResult(
    bool Passed,
    string? Reason);
