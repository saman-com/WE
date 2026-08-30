using AiGatewayService.Domain;

namespace AiGatewayService.Application;

public interface IPromptRegistry
{
    Task<PromptTemplate?> GetTemplateAsync(string promptId, string version, CancellationToken cancellationToken = default);
}

public interface IContextBuilder
{
    Task<AiContext> BuildAsync(
        string contextScope,
        string callerUserId,
        CancellationToken cancellationToken = default);
}

public interface IAiProviderAdapter
{
    Task<AiProviderResponse> CompleteAsync(
        AiProviderRequest request,
        CancellationToken cancellationToken = default);
}

public interface IResponseValidator
{
    ResponseValidationResult Validate(AiProviderResponse response);
}

public interface ISafetyFilter
{
    SafetyFilterResult Evaluate(string content);
}

public interface IAiAuditLogger
{
    Task LogAsync(
        string callerUserId,
        string promptId,
        string promptVersion,
        string contextScope,
        string promptVariablesJson,
        string aiResponse,
        string outcome,
        string? blockReason,
        string providerName,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);
}

public interface IAiAuditQueryService
{
    Task<IReadOnlyList<AiAuditLogResponse>> SearchAsync(
        AiAuditLogQuery query,
        CancellationToken cancellationToken = default);
}

public interface IAiCompletionService
{
    Task<AiCompletionResponse?> CompleteAsync(
        AiCompletionRequest request,
        string callerUserId,
        CancellationToken cancellationToken = default);
}
