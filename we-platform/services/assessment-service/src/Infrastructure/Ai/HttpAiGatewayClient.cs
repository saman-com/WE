using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssessmentService.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WePlatform.Tenancy;

namespace AssessmentService.Infrastructure.Ai;

public sealed class HttpAiGatewayClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ServiceJwtIssuer serviceJwtIssuer,
    ITenantContext tenantContext,
    ILogger<HttpAiGatewayClient> logger) : IAiGatewayClient
{
    public async Task<AiCompletionResult?> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["AiGateway:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("AI Gateway base URL is not configured.");
            return null;
        }

        if (!tenantContext.HasTenant || tenantContext.TenantId is null)
        {
            logger.LogWarning("AI Gateway request skipped because the request has no tenant.");
            return null;
        }

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/api/v1/ai/complete");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            serviceJwtIssuer.CreatePlatformServiceToken(tenantContext.TenantId.Value));
        httpRequest.Content = JsonContent.Create(request);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("AI Gateway request failed with status {StatusCode}.", response.StatusCode);
            return null;
        }

        var gatewayResponse = await response.Content.ReadFromJsonAsync<GatewayCompletionResponse>(cancellationToken);
        return gatewayResponse is null
            ? null
            : new AiCompletionResult(
                gatewayResponse.Content,
                gatewayResponse.PromptId,
                gatewayResponse.PromptVersion,
                gatewayResponse.ProviderName,
                gatewayResponse.SafetyPassed);
    }

    private sealed record GatewayCompletionResponse(
        string Content,
        string PromptId,
        string PromptVersion,
        string ProviderName,
        bool SafetyPassed);
}
