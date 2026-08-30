using WePlatform.Events;

namespace CommunicationService.Application;

public interface IDomainEventPublisher
{
    Task PublishMessageSentAsync(MessageSent domainEvent, CancellationToken cancellationToken = default);
}
