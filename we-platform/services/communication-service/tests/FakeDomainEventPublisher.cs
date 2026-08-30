using CommunicationService.Application;
using WePlatform.Events;

namespace CommunicationService.Tests;

public sealed class FakeDomainEventPublisher : IDomainEventPublisher
{
    public List<MessageSent> MessageSentEvents { get; } = [];

    public Task PublishMessageSentAsync(MessageSent domainEvent, CancellationToken cancellationToken = default)
    {
        MessageSentEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
