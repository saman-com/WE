using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AiGatewayService.Application;
using AiGatewayService.Domain;

namespace AiGatewayService.Api;

public static class AiGatewayEndpoints
{
    public static void MapAiGatewayEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/ai").RequireAuthorization();

        api.MapPost("/complete", Complete);
    }

    private static async Task<IResult> Complete(
        AiCompletionRequest request,
        ClaimsPrincipal principal,
        IAiCompletionService completionService,
        CancellationToken cancellationToken)
    {
        if (!principal.IsPlatformService())
        {
            return Results.Forbid();
        }

        var callerUserId = principal.UserId();
        var response = await completionService.CompleteAsync(request, callerUserId, cancellationToken);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsPlatformService(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.PlatformService);
}
