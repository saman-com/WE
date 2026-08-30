using Microsoft.Extensions.Logging;
using NotificationService.Application;

namespace NotificationService.Infrastructure.Email;

public sealed class MockEmailNotifier(ILogger<MockEmailNotifier> logger) : IEmailNotifier
{
    public Task SendAsync(EmailNotificationRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Mock email to {RecipientUserId}: {Subject}",
            request.RecipientUserId,
            request.Subject);
        return Task.CompletedTask;
    }
}
