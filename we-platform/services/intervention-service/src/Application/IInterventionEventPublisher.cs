using InterventionService.Domain;

namespace InterventionService.Application;

public interface IInterventionEventPublisher
{
    Task PublishAsync(Intervention intervention, CancellationToken cancellationToken = default);
}
