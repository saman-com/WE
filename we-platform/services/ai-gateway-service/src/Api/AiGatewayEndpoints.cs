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
        api.MapGet("/audit-logs", ListAuditLogs);
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

        try
        {
            var response = await completionService.CompleteAsync(request, callerUserId, cancellationToken);
            return response is null ? Results.NotFound() : Results.Ok(response);
        }
        catch (AiCompletionBlockedException blocked)
        {
            return Results.UnprocessableEntity(new AiCompletionErrorResponse(blocked.Reason));
        }
    }

    private static async Task<IResult> ListAuditLogs(
        string? promptId,
        string? outcome,
        string? callerUserId,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? limit,
        ClaimsPrincipal principal,
        IAiAuditQueryService auditQueryService,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole(PlatformRoles.SystemAdministrator))
        {
            return Results.Forbid();
        }

        var logs = await auditQueryService.SearchAsync(
            new AiAuditLogQuery(promptId, outcome, callerUserId, search, from, to, limit ?? 100),
            cancellationToken);

        return Results.Ok(logs);
    }

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsPlatformService(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.PlatformService);
}
