using EdwIngestService.Application;
using EdwIngestService.Infrastructure;
using EdwIngestService.Infrastructure.Data;
using EdwIngestService.Infrastructure.Messaging.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace EdwIngestService.Tests;

public class EdwIngestProcessorTests
{
    [Fact]
    public async Task ProcessEvidenceCreated_PersistsEvidenceFact()
    {
        await using var harness = CreateHarness();
        var evidence = CreateEvidenceCreated();

        await harness.Processor.ProcessEvidenceCreatedAsync(evidence);

        var fact = await harness.Db.EvidenceFacts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(evidence.EventId, fact.EventId);
        Assert.Equal(evidence.EvidenceId, fact.EvidenceId);
        Assert.Equal(evidence.StudentUserId, fact.StudentUserId);
        Assert.Equal(evidence.MicroSkillMarks.Count, fact.MicroSkillCount);
    }

    [Fact]
    public async Task ProcessAssessmentApproved_PersistsAssessmentFact()
    {
        await using var harness = CreateHarness();
        var assessment = CreateAssessmentApproved();

        await harness.Processor.ProcessAssessmentApprovedAsync(assessment);

        var fact = await harness.Db.AssessmentFacts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(assessment.EventId, fact.EventId);
        Assert.Equal(assessment.AssessmentId, fact.AssessmentId);
        Assert.Equal(assessment.StudentUserId, fact.StudentUserId);
        Assert.Equal(assessment.MicroSkillResults.Count, fact.MicroSkillCount);
    }

    [Fact]
    public async Task ProcessInterventionCreated_PersistsInterventionFact()
    {
        await using var harness = CreateHarness();
        var intervention = CreateInterventionCreated();

        await harness.Processor.ProcessInterventionCreatedAsync(intervention);

        var fact = await harness.Db.InterventionFacts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(intervention.EventId, fact.EventId);
        Assert.Equal(intervention.InterventionId, fact.InterventionId);
        Assert.Equal(intervention.StudentUserId, fact.StudentUserId);
        Assert.Equal(intervention.Status, fact.Status);
    }

    [Fact]
    public async Task ProcessingSameEventTwice_DoesNotDuplicateFacts()
    {
        await using var harness = CreateHarness();
        var evidence = CreateEvidenceCreated();

        await harness.Processor.ProcessEvidenceCreatedAsync(evidence);
        await harness.Processor.ProcessEvidenceCreatedAsync(evidence);

        Assert.Equal(1, await harness.Db.EvidenceFacts.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task ProcessEvidenceBatch_PersistsMultipleFactsInOneTransaction()
    {
        await using var harness = CreateHarness();
        var first = CreateEvidenceCreated();
        var second = CreateEvidenceCreated();

        await harness.Processor.ProcessEvidenceBatchAsync([first, second]);

        Assert.Equal(2, await harness.Db.EvidenceFacts.IgnoreQueryFilters().CountAsync());
        Assert.Contains(
            await harness.Db.EvidenceFacts.IgnoreQueryFilters().ToListAsync(),
            fact => fact.EventId == first.EventId);
        Assert.Contains(
            await harness.Db.EvidenceFacts.IgnoreQueryFilters().ToListAsync(),
            fact => fact.EventId == second.EventId);
    }

    private static TestHarness CreateHarness()
    {
        var tenantContext = new TenantContext();
        var options = new DbContextOptionsBuilder<EdwDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new EdwDbContext(options, tenantContext);
        return new TestHarness(db, new EdwIngestProcessor(db, tenantContext));
    }

    private sealed class TestHarness(EdwDbContext db, IEdwIngestProcessor processor) : IAsyncDisposable
    {
        public EdwDbContext Db { get; } = db;
        public IEdwIngestProcessor Processor { get; } = processor;

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

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
        await using var harness = CreateHarness();
        var testHarness = new InMemoryTestHarness();
        var consumerHarness = testHarness.Consumer(() =>
            new EvidenceCreatedConsumer(
                harness.Processor,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvidenceCreatedConsumer>.Instance));

        await testHarness.Start();
        try
        {
            var domainEvent = CreateEvidenceCreated();
            await testHarness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<EvidenceCreated>());
            var fact = await harness.Db.EvidenceFacts.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(domainEvent.EventId, fact.EventId);
        }
        finally
        {
            await testHarness.Stop();
        }
    }

    [Fact]
    public async Task AssessmentApprovedConsumer_PersistsAssessmentFact()
    {
        await using var harness = CreateHarness();
        var testHarness = new InMemoryTestHarness();
        var consumerHarness = testHarness.Consumer(() =>
            new AssessmentApprovedConsumer(
                harness.Processor,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AssessmentApprovedConsumer>.Instance));

        await testHarness.Start();
        try
        {
            var domainEvent = CreateAssessmentApproved();
            await testHarness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<AssessmentApproved>());
            var fact = await harness.Db.AssessmentFacts.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(domainEvent.EventId, fact.EventId);
        }
        finally
        {
            await testHarness.Stop();
        }
    }

    [Fact]
    public async Task InterventionCreatedConsumer_PersistsInterventionFact()
    {
        await using var harness = CreateHarness();
        var testHarness = new InMemoryTestHarness();
        var consumerHarness = testHarness.Consumer(() =>
            new InterventionCreatedConsumer(
                harness.Processor,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<InterventionCreatedConsumer>.Instance));

        await testHarness.Start();
        try
        {
            var domainEvent = CreateInterventionCreated();
            await testHarness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<InterventionCreated>());
            var fact = await harness.Db.InterventionFacts.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(domainEvent.EventId, fact.EventId);
        }
        finally
        {
            await testHarness.Stop();
        }
    }

    private static TestHarness CreateHarness()
    {
        var tenantContext = new TenantContext();
        var options = new DbContextOptionsBuilder<EdwDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new EdwDbContext(options, tenantContext);
        return new TestHarness(db, new EdwIngestProcessor(db, tenantContext));
    }

    private sealed class TestHarness(EdwDbContext db, IEdwIngestProcessor processor) : IAsyncDisposable
    {
        public EdwDbContext Db { get; } = db;
        public IEdwIngestProcessor Processor { get; } = processor;

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

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
