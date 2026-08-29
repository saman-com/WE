using MassTransit;
using WePlatform.Events;

namespace EvidenceService.Infrastructure.Messaging;

public sealed class MassTransitDomainEventPublisher(IPublishEndpoint publishEndpoint) : Application.IDomainEventPublisher
{
    public Task PublishEvidenceCreatedAsync(EvidenceCreated domainEvent, CancellationToken cancellationToken = default) =>
        publishEndpoint.Publish(domainEvent, context => context.SetRoutingKey(EvidenceCreated.EventType), cancellationToken);

    public Task PublishAssessmentApprovedAsync(AssessmentApproved domainEvent, CancellationToken cancellationToken = default) =>
        publishEndpoint.Publish(domainEvent, context => context.SetRoutingKey(AssessmentApproved.EventType), cancellationToken);
}
