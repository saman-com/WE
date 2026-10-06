using InterventionService.Application;
using InterventionService.Domain;

namespace InterventionService.Infrastructure.Messaging;

public sealed class NullInterventionEventPublisher : IInterventionEventPublisher
{
    public Task PublishAsync(Intervention intervention, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
