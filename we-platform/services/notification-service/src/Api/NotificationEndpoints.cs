using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Api;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/notifications").RequireAuthorization();

        api.MapGet("/", ListNotifications);
        api.MapPatch("/{notificationId:guid}/read", MarkAsRead);
    }

    private static async Task<IResult> ListNotifications(
        ClaimsPrincipal principal,
        NotificationDbContext db)
    {
        var userId = principal.UserId();
        var notifications = await db.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Results.Ok(new NotificationListResponse(notifications.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> MarkAsRead(
        Guid notificationId,
        ClaimsPrincipal principal,
        NotificationDbContext db)
    {
        var notification = await db.Notifications.FindAsync(notificationId);
        if (notification is null)
        {
            return Results.NotFound();
        }

        if (notification.RecipientUserId != principal.UserId())
        {
            return Results.Forbid();
        }

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return Results.Ok(ToResponse(notification));
    }

    private static NotificationResponse ToResponse(Notification notification) =>
        new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.RelatedEntityId,
            notification.ReadAt is not null,
            notification.CreatedAt,
            notification.ReadAt);

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;
}
