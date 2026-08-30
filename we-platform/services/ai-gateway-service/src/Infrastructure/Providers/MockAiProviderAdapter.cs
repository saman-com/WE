using AiGatewayService.Application;

namespace AiGatewayService.Infrastructure.Providers;

public sealed class MockAiProviderAdapter : IAiProviderAdapter
{
    public Task<AiProviderResponse> CompleteAsync(
        AiProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var content =
            "Draft feedback (mock provider): Your work on this micro-skill shows progress. " +
            "Review the evidence summary and continue practising with guided support.";

        return Task.FromResult(new AiProviderResponse(content, "Mock"));
    }
}
