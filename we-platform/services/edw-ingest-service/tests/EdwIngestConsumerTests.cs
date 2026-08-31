using EdwIngestService.Application;
using EdwIngestService.Infrastructure;
using EdwIngestService.Infrastructure.Data;
using EdwIngestService.Infrastructure.Messaging.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Events;

namespace EdwIngestService.Tests;

public class EdwIngestProcessorTests
{
    [Fact]
    public async Task ProcessEvidenceCreated_PersistsEvidenceFact()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var evidence = CreateEvidenceCreated();

        await processor.ProcessEvidenceCreatedAsync(evidence);

        var fact = await context.EvidenceFacts.SingleAsync();
        Assert.Equal(evidence.EventId, fact.EventId);
        Assert.Equal(evidence.EvidenceId, fact.EvidenceId);
        Assert.Equal(evidence.StudentUserId, fact.StudentUserId);
        Assert.Equal(evidence.MicroSkillMarks.Count, fact.MicroSkillCount);
    }

    [Fact]
    public async Task ProcessAssessmentApproved_PersistsAssessmentFact()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var assessment = CreateAssessmentApproved();

        await processor.ProcessAssessmentApprovedAsync(assessment);

        var fact = await context.AssessmentFacts.SingleAsync();
        Assert.Equal(assessment.EventId, fact.EventId);
        Assert.Equal(assessment.AssessmentId, fact.AssessmentId);
        Assert.Equal(assessment.StudentUserId, fact.StudentUserId);
        Assert.Equal(assessment.MicroSkillResults.Count, fact.MicroSkillCount);
    }

    [Fact]
    public async Task ProcessInterventionCreated_PersistsInterventionFact()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var intervention = CreateInterventionCreated();

        await processor.ProcessInterventionCreatedAsync(intervention);

        var fact = await context.InterventionFacts.SingleAsync();
        Assert.Equal(intervention.EventId, fact.EventId);
        Assert.Equal(intervention.InterventionId, fact.InterventionId);
        Assert.Equal(intervention.StudentUserId, fact.StudentUserId);
        Assert.Equal(intervention.Status, fact.Status);
    }

    [Fact]
    public async Task ProcessingSameEventTwice_DoesNotDuplicateFacts()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var evidence = CreateEvidenceCreated();

        await processor.ProcessEvidenceCreatedAsync(evidence);
        await processor.ProcessEvidenceCreatedAsync(evidence);

        Assert.Equal(1, await context.EvidenceFacts.CountAsync());
    }

    [Fact]
    public async Task ProcessEvidenceBatch_PersistsMultipleFactsInOneTransaction()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var first = CreateEvidenceCreated();
        var second = CreateEvidenceCreated();

        await processor.ProcessEvidenceBatchAsync([first, second]);

        Assert.Equal(2, await context.EvidenceFacts.CountAsync());
        Assert.Contains(await context.EvidenceFacts.ToListAsync(), fact => fact.EventId == first.EventId);
        Assert.Contains(await context.EvidenceFacts.ToListAsync(), fact => fact.EventId == second.EventId);
    }

    private static EdwDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EdwDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EdwDbContext(options);
    }

    private static IEdwIngestProcessor CreateProcessor(EdwDbContext context) =>
        new EdwIngestProcessor(context);

    private static EvidenceCreated CreateEvidenceCreated()
    {
        var eventId = Guid.CreateVersion7();
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
            [new MicroSkillResult(Guid.CreateVersion7(), 4, "Strong work.")]);
    }

    private static AssessmentApproved CreateAssessmentApproved()
    {
        var eventId = Guid.CreateVersion7();
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
            [new MicroSkillResult(Guid.CreateVersion7(), 3, "Developing.")]);
    }

    private static InterventionCreated CreateInterventionCreated()
    {
        var eventId = Guid.CreateVersion7();
        return new InterventionCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            InterventionCreated.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            "Planned",
            DateTimeOffset.UtcNow);
    }
}

public class EdwIngestConsumerTests
{
    [Fact]
    public async Task EvidenceCreatedConsumer_PersistsEvidenceFact()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new EvidenceCreatedConsumer(
                processor,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvidenceCreatedConsumer>.Instance));

        await harness.Start();
        try
        {
            var domainEvent = CreateEvidenceCreated();
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<EvidenceCreated>());
            var fact = await context.EvidenceFacts.SingleAsync();
            Assert.Equal(domainEvent.EventId, fact.EventId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task AssessmentApprovedConsumer_PersistsAssessmentFact()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new AssessmentApprovedConsumer(
                processor,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AssessmentApprovedConsumer>.Instance));

        await harness.Start();
        try
        {
            var domainEvent = CreateAssessmentApproved();
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<AssessmentApproved>());
            var fact = await context.AssessmentFacts.SingleAsync();
            Assert.Equal(domainEvent.EventId, fact.EventId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task InterventionCreatedConsumer_PersistsInterventionFact()
    {
        await using var context = CreateDbContext();
        var processor = CreateProcessor(context);
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new InterventionCreatedConsumer(
                processor,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<InterventionCreatedConsumer>.Instance));

        await harness.Start();
        try
        {
            var domainEvent = CreateInterventionCreated();
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<InterventionCreated>());
            var fact = await context.InterventionFacts.SingleAsync();
            Assert.Equal(domainEvent.EventId, fact.EventId);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static EdwDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EdwDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EdwDbContext(options);
    }

    private static IEdwIngestProcessor CreateProcessor(EdwDbContext context) =>
        new EdwIngestProcessor(context);

    private static EvidenceCreated CreateEvidenceCreated()
    {
        var eventId = Guid.CreateVersion7();
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
            [new MicroSkillResult(Guid.CreateVersion7(), 4, "Strong work.")]);
    }

    private static AssessmentApproved CreateAssessmentApproved()
    {
        var eventId = Guid.CreateVersion7();
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
            [new MicroSkillResult(Guid.CreateVersion7(), 3, "Developing.")]);
    }

    private static InterventionCreated CreateInterventionCreated()
    {
        var eventId = Guid.CreateVersion7();
        return new InterventionCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            InterventionCreated.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            "Planned",
            DateTimeOffset.UtcNow);
    }
}
