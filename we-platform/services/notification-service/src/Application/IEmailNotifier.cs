namespace NotificationService.Application;

public interface IEmailNotifier
{
    Task SendAsync(EmailNotificationRequest request, CancellationToken cancellationToken = default);
}
