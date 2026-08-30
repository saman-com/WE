using System.Text.RegularExpressions;
using AiGatewayService.Application;

namespace AiGatewayService.Application;

public sealed partial class AiCompletionService(
    IPromptRegistry promptRegistry,
    IContextBuilder contextBuilder,
    IAiProviderAdapter providerAdapter,
    IResponseValidator responseValidator,
    ISafetyFilter safetyFilter) : IAiCompletionService
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
            throw new InvalidOperationException(validation.Reason ?? "AI response failed validation.");
        }

        var safety = safetyFilter.Evaluate(providerResponse.Content);
        if (!safety.Passed)
        {
            throw new InvalidOperationException(safety.Reason ?? "AI response failed safety checks.");
        }

        return new AiCompletionResponse(
            providerResponse.Content,
            template.Metadata.Id,
            template.Metadata.Version,
            providerResponse.ProviderName,
            safety.Passed);
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
