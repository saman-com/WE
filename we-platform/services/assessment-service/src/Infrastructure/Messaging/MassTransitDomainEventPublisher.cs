using MassTransit;
using WePlatform.Events;

namespace AssessmentService.Infrastructure.Messaging;

public sealed class MassTransitDomainEventPublisher(IPublishEndpoint publishEndpoint) : Application.IDomainEventPublisher
{
    public Task PublishAssessmentPublishedAsync(AssessmentPublished domainEvent, CancellationToken cancellationToken = default) =>
        publishEndpoint.Publish(domainEvent, context => context.SetRoutingKey(AssessmentPublished.EventType), cancellationToken);
}
