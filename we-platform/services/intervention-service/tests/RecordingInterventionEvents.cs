using InterventionService.Application;
using InterventionService.Domain;

namespace InterventionService.Tests;

public sealed class RecordingInterventionEvents : IInterventionEventPublisher
{
    public List<(Guid Id, string Status, Guid LearningGapId)> Published { get; } = [];

    public Task PublishAsync(Intervention intervention, CancellationToken cancellationToken = default)
    {
        Published.Add((intervention.Id, intervention.Status, intervention.LearningGapId));
        return Task.CompletedTask;
    }
}
