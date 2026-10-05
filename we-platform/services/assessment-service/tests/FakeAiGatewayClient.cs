using AssessmentService.Application;

namespace AssessmentService.Tests;

public sealed class FakeAiGatewayClient : IAiGatewayClient
{
    public string DraftContent { get; set; } =
        "Draft feedback (mock): Your work shows progress. Consider reviewing the key concepts.";

    public List<AiCompletionCall> Calls { get; } = [];

    public Task<AiCompletionResult?> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new AiCompletionCall(request.PromptId, request.PromptVersion, request.Variables));
        return Task.FromResult<AiCompletionResult?>(new AiCompletionResult(
            DraftContent,
            request.PromptId,
            request.PromptVersion,
            "Mock",
            true));
    }
}

public sealed record AiCompletionCall(
    string PromptId,
    string PromptVersion,
    IReadOnlyDictionary<string, string> Variables);
