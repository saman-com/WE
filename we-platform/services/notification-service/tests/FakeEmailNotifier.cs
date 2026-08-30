using NotificationService.Application;

namespace NotificationService.Tests;

public sealed class FakeEmailNotifier : IEmailNotifier
{
    public List<EmailNotificationRequest> Sent { get; } = [];

    public Task SendAsync(EmailNotificationRequest request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        return Task.CompletedTask;
    }
}
