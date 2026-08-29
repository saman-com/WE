using EventSubscriberService.Api.Consumers;
using MassTransit;
using MassTransit.Testing;
using WePlatform.Events;

namespace EventSubscriberService.Tests;

public class DomainEventConsumerTests
{
    [Fact]
    public async Task EvidenceCreatedConsumer_ReceivesPublishedEvent()
    {
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new EvidenceCreatedConsumer(Microsoft.Extensions.Logging.Abstractions.NullLogger<EvidenceCreatedConsumer>.Instance));

        await harness.Start();
        try
        {
            var domainEvent = CreateEvidenceCreated();

            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<EvidenceCreated>());
            var context = consumerHarness.Consumed.Select<EvidenceCreated>().First().Context;
            Assert.Equal(domainEvent.EventId, context.Message.EventId);
            Assert.Equal(domainEvent.EvidenceId, context.Message.EvidenceId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task AssessmentApprovedConsumer_ReceivesPublishedEventWithMicroSkillResults()
    {
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() => new AssessmentApprovedConsumer(Microsoft.Extensions.Logging.Abstractions.NullLogger<AssessmentApprovedConsumer>.Instance));

        await harness.Start();
        try
        {
            var domainEvent = CreateAssessmentApproved();

            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<AssessmentApproved>());
            var context = consumerHarness.Consumed.Select<AssessmentApproved>().First().Context;
            Assert.Equal(domainEvent.AssessmentId, context.Message.AssessmentId);
            Assert.Equal(domainEvent.StudentUserId, context.Message.StudentUserId);
            Assert.Single(context.Message.MicroSkillResults);
            Assert.Equal(4, context.Message.MicroSkillResults[0].Mark);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static EvidenceCreated CreateEvidenceCreated()
    {
        var eventId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        return new EvidenceCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            EvidenceCreated.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new MicroSkillResult(microSkillId, 4, "Strong work.")]);
    }

    private static AssessmentApproved CreateAssessmentApproved()
    {
        var eventId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        return new AssessmentApproved(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            AssessmentApproved.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new MicroSkillResult(microSkillId, 4, "Strong work.")]);
    }
}
