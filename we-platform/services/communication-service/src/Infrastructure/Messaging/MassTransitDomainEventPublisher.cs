using MassTransit;
using WePlatform.Events;

namespace CommunicationService.Infrastructure.Messaging;

public sealed class MassTransitDomainEventPublisher(IPublishEndpoint publishEndpoint) : Application.IDomainEventPublisher
{
    public Task PublishMessageSentAsync(MessageSent domainEvent, CancellationToken cancellationToken = default) =>
        publishEndpoint.Publish(domainEvent, context => context.SetRoutingKey(MessageSent.EventType), cancellationToken);
}
