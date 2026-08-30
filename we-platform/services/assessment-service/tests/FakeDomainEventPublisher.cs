using AssessmentService.Application;
using WePlatform.Events;

namespace AssessmentService.Tests;

public sealed class FakeDomainEventPublisher : IDomainEventPublisher
{
    public List<AssessmentPublished> AssessmentPublishedEvents { get; } = [];

    public Task PublishAssessmentPublishedAsync(AssessmentPublished domainEvent, CancellationToken cancellationToken = default)
    {
        AssessmentPublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
