using System.Net;
using System.Net.Http.Json;
using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Tenancy;

namespace NotificationService.Tests;

public class NotificationEndpointTests : IClassFixture<NotificationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly NotificationWebApplicationFactory _factory;

    public NotificationEndpointTests(NotificationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task User_CanListOwnNotifications()
    {
        var userId = Guid.NewGuid().ToString();
        await SeedNotificationAsync(userId, NotificationTypes.AssessmentPublished, "Algebra quiz available");

        using var request = TestJwt.Authorized(HttpMethod.Get, "/api/v1/notifications", userId, TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<NotificationListResponse>();
        var notification = Assert.Single(list!.Notifications);
        Assert.Equal(NotificationTypes.AssessmentPublished, notification.Type);
        Assert.Equal("Algebra quiz available", notification.Title);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task User_CanMarkNotificationAsRead()
    {
        var userId = Guid.NewGuid().ToString();
        var notificationId = await SeedNotificationAsync(userId, NotificationTypes.NewMessage, "New parent message");

        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/notifications/{notificationId}/read",
            userId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        Assert.True(updated!.IsRead);
        Assert.NotNull(updated.ReadAt);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var persisted = await db.Notifications.FindAsync(notificationId);
        Assert.NotNull(persisted!.ReadAt);
    }

    [Fact]
    public async Task UnrelatedUser_CannotMarkNotificationAsRead()
    {
        var recipientId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var notificationId = await SeedNotificationAsync(recipientId, NotificationTypes.FeedbackAvailable, "Feedback ready");

        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/notifications/{notificationId}/read",
            otherUserId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Notifications_AreScopedToRecipientOnly()
    {
        var userOneId = Guid.NewGuid().ToString();
        var userTwoId = Guid.NewGuid().ToString();
        await SeedNotificationAsync(userOneId, NotificationTypes.NewMessage, "Message for user one");
        await SeedNotificationAsync(userTwoId, NotificationTypes.NewMessage, "Message for user two");

        using var request = TestJwt.Authorized(HttpMethod.Get, "/api/v1/notifications", userOneId, TestJwt.ParentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<NotificationListResponse>();
        var notification = Assert.Single(list!.Notifications);
        Assert.Equal("Message for user one", notification.Title);
    }

    private async Task<Guid> SeedNotificationAsync(string recipientUserId, string type, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            TenantId = DefaultTenant.Id,
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Body = $"{title} details",
            SourceEventId = Guid.CreateVersion7(),
            SourceEventType = type,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
        return notification.Id;
    }
}
