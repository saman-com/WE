using System.Text.Json;
using System.Text.RegularExpressions;
using AiGatewayService.Domain;

namespace AiGatewayService.Application;

public sealed partial class AiCompletionService(
    IPromptRegistry promptRegistry,
    IContextBuilder contextBuilder,
    IAiProviderAdapter providerAdapter,
    IResponseValidator responseValidator,
    ISafetyFilter safetyFilter,
    IAiAuditLogger auditLogger) : IAiCompletionService
{
    public async Task<AiCompletionResponse?> CompleteAsync(
        AiCompletionRequest request,
        string callerUserId,
        CancellationToken cancellationToken = default)
    {
        var template = await promptRegistry.GetTemplateAsync(
            request.PromptId,
            request.PromptVersion,
            cancellationToken);

        if (template is null)
        {
            return null;
        }

        var renderedPrompt = RenderTemplate(template.Body, request.Variables);
        var variablesJson = JsonSerializer.Serialize(request.Variables);
        var auditContent = $"{renderedPrompt}\n{string.Join('\n', request.Variables.Values)}";
        var createdAt = DateTimeOffset.UtcNow;

        var inputSafety = safetyFilter.Evaluate(auditContent);
        if (!inputSafety.Passed)
        {
            await auditLogger.LogAsync(
                callerUserId,
                template.Metadata.Id,
                template.Metadata.Version,
                request.ContextScope,
                variablesJson,
                string.Empty,
                AiAuditOutcomes.Blocked,
                inputSafety.Reason,
                "None",
                createdAt,
                cancellationToken);

            throw new AiCompletionBlockedException(inputSafety.Reason ?? "Prompt failed safety checks.");
        }

        var context = await contextBuilder.BuildAsync(
            request.ContextScope,
            callerUserId,
            cancellationToken);

        var providerResponse = await providerAdapter.CompleteAsync(
            new AiProviderRequest(renderedPrompt, context),
            cancellationToken);

        var validation = responseValidator.Validate(providerResponse);
        if (!validation.IsValid)
        {
            await auditLogger.LogAsync(
                callerUserId,
                template.Metadata.Id,
                template.Metadata.Version,
                request.ContextScope,
                variablesJson,
                providerResponse.Content,
                AiAuditOutcomes.ValidationFailed,
                validation.Reason,
                providerResponse.ProviderName,
                createdAt,
                cancellationToken);

            throw new InvalidOperationException(validation.Reason ?? "AI response failed validation.");
        }

        var outputSafety = safetyFilter.Evaluate(providerResponse.Content);
        if (!outputSafety.Passed)
        {
            await auditLogger.LogAsync(
                callerUserId,
                template.Metadata.Id,
                template.Metadata.Version,
                request.ContextScope,
                variablesJson,
                providerResponse.Content,
                AiAuditOutcomes.Blocked,
                outputSafety.Reason,
                providerResponse.ProviderName,
                createdAt,
                cancellationToken);

            throw new AiCompletionBlockedException(outputSafety.Reason ?? "AI response failed safety checks.");
        }

        await auditLogger.LogAsync(
            callerUserId,
            template.Metadata.Id,
            template.Metadata.Version,
            request.ContextScope,
            variablesJson,
            providerResponse.Content,
            AiAuditOutcomes.Success,
            null,
            providerResponse.ProviderName,
            createdAt,
            cancellationToken);

        return new AiCompletionResponse(
            providerResponse.Content,
            template.Metadata.Id,
            template.Metadata.Version,
            providerResponse.ProviderName,
            outputSafety.Passed);
    }

    private static string RenderTemplate(string body, IReadOnlyDictionary<string, string> variables)
    {
        return VariableToken().Replace(body, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariableToken();
}
