using AiGatewayService.Application;

namespace AiGatewayService.Infrastructure.Safety;

public sealed class StubSafetyFilter : ISafetyFilter
{
    public SafetyFilterResult Evaluate(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SafetyFilterResult(false, "Content is empty.");
        }

        return new SafetyFilterResult(true, null);
    }
}
