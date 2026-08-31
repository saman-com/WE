using System.Net;
using System.Net.Http.Json;
using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace NotificationService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<NotificationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly NotificationWebApplicationFactory _factory;

    public TenantIsolationEndpointTests(NotificationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task User_FromDifferentTenant_CannotMarkNotificationAsRead()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var recipientId = Guid.NewGuid().ToString();
        var notificationId = await SeedNotificationAsync(recipientId, tenantB);

        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/notifications/{notificationId}/read",
            recipientId,
            tenantA,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_FromDifferentTenant_DoesNotSeeOtherTenantNotifications()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var userId = Guid.NewGuid().ToString();
        await SeedNotificationAsync(userId, tenantB, "Cross-tenant notification");

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/notifications",
            userId,
            tenantA,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<NotificationListResponse>();
        Assert.Empty(list!.Notifications);
    }

    private async Task<Guid> SeedNotificationAsync(
        string recipientUserId,
        Guid tenantId,
        string title = "Tenant isolation test")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            RecipientUserId = recipientUserId,
            Type = NotificationTypes.NewMessage,
            Title = title,
            Body = $"{title} details",
            SourceEventId = Guid.CreateVersion7(),
            SourceEventType = NotificationTypes.NewMessage,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification.Id;
    }
}
