using System.Net;
using System.Net.Http.Json;
using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure.Data;
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
    public async Task CrossSchool_NotificationList_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: notification-service:notification-list
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var userId = Guid.NewGuid().ToString();
        await SeedNotificationAsync(userId, schoolA, "School A notification");
        await SeedNotificationAsync(userId, schoolB, "School B notification");

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/notifications",
            userId,
            schoolA,
            TestJwt.StudentRole);
        var okA = await _client.SendAsync(listA);
        okA.EnsureSuccessStatusCode();
        var itemsA = await okA.Content.ReadFromJsonAsync<NotificationListResponse>();
        Assert.Single(itemsA!.Notifications);
        Assert.Contains(itemsA.Notifications, n => n.Title == "School A notification");
        Assert.DoesNotContain(itemsA.Notifications, n => n.Title == "School B notification");

        using var listB = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/notifications",
            userId,
            schoolB,
            TestJwt.StudentRole);
        var okB = await _client.SendAsync(listB);
        okB.EnsureSuccessStatusCode();
        var itemsB = await okB.Content.ReadFromJsonAsync<NotificationListResponse>();
        Assert.Single(itemsB!.Notifications);
        Assert.Contains(itemsB.Notifications, n => n.Title == "School B notification");
        Assert.DoesNotContain(itemsB.Notifications, n => n.Title == "School A notification");
    }

    [Fact]
    public async Task CrossSchool_NotificationResource_BothDirections_Denied()
    {
        // Probe: notification-service:notification-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var userId = Guid.NewGuid().ToString();
        var notificationA = await SeedNotificationAsync(userId, schoolA);
        var notificationB = await SeedNotificationAsync(userId, schoolB);

        await AssertMarkReadDeniedAsync(userId, schoolB, notificationA);
        await AssertMarkReadDeniedAsync(userId, schoolA, notificationB);
    }

    private async Task AssertMarkReadDeniedAsync(string userId, Guid callerTenant, Guid notificationId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/notifications/{notificationId}/read",
            userId,
            callerTenant,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
