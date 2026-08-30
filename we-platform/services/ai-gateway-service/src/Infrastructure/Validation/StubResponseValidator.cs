using AiGatewayService.Application;

namespace AiGatewayService.Infrastructure.Validation;

public sealed class StubResponseValidator : IResponseValidator
{
    public ResponseValidationResult Validate(AiProviderResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return new ResponseValidationResult(false, "Response content is empty.");
        }

        return new ResponseValidationResult(true, null);
    }
}
