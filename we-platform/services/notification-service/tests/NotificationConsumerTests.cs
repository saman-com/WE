using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application;
using NotificationService.Domain;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Data;
using NotificationService.Infrastructure.Messaging.Consumers;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace NotificationService.Tests;

public class NotificationConsumerTests
{
    [Fact]
    public async Task AssessmentPublishedConsumer_CreatesInAppNotificationAndSendsEmail()
    {
        var studentId = Guid.NewGuid().ToString();
        var emailNotifier = new FakeEmailNotifier();
        await using var provider = BuildProvider(emailNotifier);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new AssessmentPublishedConsumer(provider.GetRequiredService<INotificationCreator>()));

        await harness.Start();
        try
        {
            var domainEvent = CreateAssessmentPublished(studentId);
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<AssessmentPublished>());

            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var notification = await db.Notifications.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(studentId, notification.RecipientUserId);
            Assert.Equal(NotificationTypes.AssessmentPublished, notification.Type);
            Assert.Equal(domainEvent.EventId, notification.SourceEventId);
            Assert.Equal(domainEvent.AssessmentId, notification.RelatedEntityId);
            Assert.Equal(domainEvent.OrganisationId, notification.TenantId);

            var email = Assert.Single(emailNotifier.Sent);
            Assert.Equal(studentId, email.RecipientUserId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task AssessmentApprovedConsumer_CreatesFeedbackAvailableNotification()
    {
        var studentId = Guid.NewGuid().ToString();
        var emailNotifier = new FakeEmailNotifier();
        await using var provider = BuildProvider(emailNotifier);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new AssessmentApprovedConsumer(provider.GetRequiredService<INotificationCreator>()));

        await harness.Start();
        try
        {
            var domainEvent = CreateAssessmentApproved(studentId);
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<AssessmentApproved>());

            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var notification = await db.Notifications.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(studentId, notification.RecipientUserId);
            Assert.Equal(NotificationTypes.FeedbackAvailable, notification.Type);
            Assert.Equal(domainEvent.AssessmentId, notification.RelatedEntityId);
            Assert.Equal(domainEvent.OrganisationId, notification.TenantId);
            Assert.NotEmpty(emailNotifier.Sent);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task MessageSentConsumer_CreatesNewMessageNotification()
    {
        var recipientId = Guid.NewGuid().ToString();
        var emailNotifier = new FakeEmailNotifier();
        await using var provider = BuildProvider(emailNotifier);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new MessageSentConsumer(provider.GetRequiredService<INotificationCreator>()));

        await harness.Start();
        try
        {
            var domainEvent = CreateMessageSent(recipientId);
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<MessageSent>());

            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            var notification = await db.Notifications.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(recipientId, notification.RecipientUserId);
            Assert.Equal(NotificationTypes.NewMessage, notification.Type);
            Assert.Equal(domainEvent.MessageId, notification.RelatedEntityId);
            Assert.Equal(DefaultTenant.Id, notification.TenantId);
            Assert.NotEmpty(emailNotifier.Sent);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static ServiceProvider BuildProvider(FakeEmailNotifier emailNotifier)
    {
        var services = new ServiceCollection();
        services.AddWePlatformTenancy();
        services.AddDbContext<NotificationDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            ServiceLifetime.Singleton);
        services.AddSingleton<IEmailNotifier>(emailNotifier);
        services.AddSingleton<INotificationCreator, NotificationCreator>();
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<NotificationDbContext>().Database.EnsureCreated();
        return provider;
    }

    private static AssessmentPublished CreateAssessmentPublished(string studentId)
    {
        var eventId = Guid.CreateVersion7();
        return new AssessmentPublished(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            AssessmentPublished.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Term quiz",
            Guid.NewGuid().ToString(),
            [studentId]);
    }

    private static AssessmentApproved CreateAssessmentApproved(string studentId)
    {
        var eventId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        return new AssessmentApproved(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            AssessmentApproved.CurrentVersion,
            Guid.CreateVersion7(),
            studentId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new MicroSkillResult(microSkillId, 4, "Strong work.")]);
    }

    private static MessageSent CreateMessageSent(string recipientId)
    {
        var eventId = Guid.CreateVersion7();
        return new MessageSent(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            MessageSent.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            recipientId,
            "Could we discuss progress?");
    }
}
