namespace AssessmentService.Application;

public interface IAiGatewayClient
{
    Task<AiCompletionResult?> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default);
}
