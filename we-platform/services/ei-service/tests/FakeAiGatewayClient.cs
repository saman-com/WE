using EiService.Application;

namespace EiService.Tests;

public sealed class FakeAiGatewayClient : IAiGatewayClient
{
    public string DraftContent { get; set; } =
        "Draft summary (mock): Class progress shows developing mastery. Continue targeted practice.";

    public List<AiCompletionCall> Calls { get; } = [];

    public Task<AiCompletionResult?> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new AiCompletionCall(
            request.PromptId,
            request.PromptVersion,
            request.ContextScope,
            request.Variables));
        return Task.FromResult<AiCompletionResult?>(new AiCompletionResult(
            DraftContent,
            request.PromptId,
            request.PromptVersion,
            "mock",
            true));
    }
}

public sealed record AiCompletionCall(
    string PromptId,
    string PromptVersion,
    string ContextScope,
    IReadOnlyDictionary<string, string> Variables);
