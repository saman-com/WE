using EvidenceService.Application;
using WePlatform.Events;

namespace EvidenceService.Tests;

public sealed class FakeDomainEventPublisher : IDomainEventPublisher
{
    public List<EvidenceCreated> EvidenceCreatedEvents { get; } = [];

    public List<AssessmentApproved> AssessmentApprovedEvents { get; } = [];

    public Task PublishEvidenceCreatedAsync(EvidenceCreated domainEvent, CancellationToken cancellationToken = default)
    {
        EvidenceCreatedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }

    public Task PublishAssessmentApprovedAsync(AssessmentApproved domainEvent, CancellationToken cancellationToken = default)
    {
        AssessmentApprovedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
