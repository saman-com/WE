using InterventionService.Application;
using InterventionService.Domain;
using MassTransit;
using WePlatform.Events;

namespace InterventionService.Infrastructure.Messaging;

public sealed class MassTransitInterventionEventPublisher(IPublishEndpoint publishEndpoint) : IInterventionEventPublisher
{
    public Task PublishAsync(Intervention intervention, CancellationToken cancellationToken = default)
    {
        var eventId = Guid.CreateVersion7();
        var domainEvent = new InterventionCreated(
            eventId,
            eventId,
            intervention.UpdatedAt == default ? intervention.CreatedAt : intervention.UpdatedAt,
            intervention.OrganisationId,
            InterventionCreated.CurrentVersion,
            intervention.Id,
            intervention.StudentUserId,
            intervention.LearningGapId,
            intervention.AssignedTeacherUserId,
            intervention.Status,
            intervention.CreatedAt);

        return publishEndpoint.Publish(
            domainEvent,
            context => context.SetRoutingKey(InterventionCreated.EventType),
            cancellationToken);
    }
}
