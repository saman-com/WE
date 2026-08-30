using AiGatewayService.Application;

namespace AiGatewayService.Infrastructure.Context;

public sealed class StubContextBuilder : IContextBuilder
{
    public Task<AiContext> BuildAsync(
        string contextScope,
        string callerUserId,
        CancellationToken cancellationToken = default)
    {
        var items = new Dictionary<string, string>
        {
            ["scope"] = contextScope,
            ["callerUserId"] = callerUserId
        };

        return Task.FromResult(new AiContext(contextScope, items));
    }
}
