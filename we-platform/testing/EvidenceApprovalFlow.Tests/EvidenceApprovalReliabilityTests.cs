using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using WePlatform.Events;
using WePlatform.Tenancy;
using Xunit;
using ExchangeType = RabbitMQ.Client.ExchangeType;

namespace EvidenceApprovalFlow.Tests;

[Collection(FlowCollection.Name)]
public sealed class EvidenceApprovalReliabilityTests(FlowFixture fixture)
{
    [Fact]
    public async Task RabbitMqDownDuringApproval_EvidenceSavedAndDeliveredAfterRecovery()
    {
        var organisationId = DefaultTenant.Id;
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var classId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();

        await fixture.SeedStudentProfileAsync(organisationId, studentId);
        await fixture.StopRabbitAsync();

        var evidenceId = await fixture.ApproveEvidenceAsync(
            organisationId,
            teacherId,
            studentId,
            classId,
            assessmentId,
            submissionId,
            "Outbox recovery",
            [(microSkillId, 5m, "ok")]);

        Assert.Equal(1, await fixture.ScalarAsync("we_evidence_it",
            $"select count(*) from educational_evidence where id = '{evidenceId}'"));

        var outboxTables = await fixture.ScalarAsync("we_evidence_it",
            "select count(*) from information_schema.tables where table_schema = 'public' and table_name in ('OutboxMessage','outbox_message')");
        Assert.True(outboxTables >= 1);

        Assert.Equal(0, await fixture.ScalarAsync("we_learning_it",
            $"select count(*) from profile_evidence_entries where id = '{evidenceId}'"));

        await fixture.RecoverRabbitAndRestartHostsAsync();

        await fixture.WaitForAsync(async () =>
            await fixture.ScalarAsync("we_learning_it",
                $"select count(*) from profile_evidence_entries where id = '{evidenceId}'") == 1
            && await fixture.ScalarAsync("we_diagnostic_it",
                $"select count(*) from micro_skill_diagnostics where evidence_id = '{evidenceId}'") == 1
            && await fixture.ScalarAsync("we_mastery_it",
                $"select count(*) from mastery_evidence_marks where evidence_id = '{evidenceId}'") == 1,
            TimeSpan.FromSeconds(90));
    }

    [Fact]
    public async Task DuplicateEvidenceCreated_DoesNotDuplicateDiagnosticRows()
    {
        var organisationId = DefaultTenant.Id;
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var domainEvent = new EvidenceCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            organisationId,
            EvidenceCreated.CurrentVersion,
            evidenceId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            studentId,
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new MicroSkillResult(microSkillId, 2m, "Developing")],
            "Duplicate check");

        var provider = BuildPublishOnlyBus(fixture);
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            await Task.Delay(500);
            await bus.Publish(domainEvent, ctx => ctx.SetRoutingKey(EvidenceCreated.EventType));
            await bus.Publish(domainEvent, ctx => ctx.SetRoutingKey(EvidenceCreated.EventType));

            await fixture.WaitForAsync(async () =>
                await fixture.ScalarAsync("we_diagnostic_it",
                    $"select count(*) from micro_skill_diagnostics where evidence_id = '{evidenceId}'") == 1);

            await Task.Delay(1000);
            Assert.Equal(1, await fixture.ScalarAsync("we_diagnostic_it",
                $"select count(*) from micro_skill_diagnostics where evidence_id = '{evidenceId}'"));
        }
        finally
        {
            await bus.StopAsync();
            await provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task ConsumerException_RetriesThenSucceeds()
    {
        var attempts = 0;
        var harness = new InMemoryTestHarness();
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.UseMessageRetry(r =>
                r.Intervals(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50)));
        };

        var consumer = harness.Consumer(() =>
            new DelegateEvidenceConsumer(_ =>
            {
                var current = Interlocked.Increment(ref attempts);
                if (current == 1)
                {
                    throw new InvalidOperationException("Transient failure");
                }

                return Task.CompletedTask;
            }));

        await harness.Start();
        try
        {
            var eventId = Guid.CreateVersion7();
            await harness.InputQueueSendEndpoint.Send(new EvidenceCreated(
                eventId,
                eventId,
                DateTimeOffset.UtcNow,
                DefaultTenant.Id,
                EvidenceCreated.CurrentVersion,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.NewGuid().ToString(),
                Guid.CreateVersion7(),
                Guid.NewGuid().ToString(),
                DateTimeOffset.UtcNow,
                [new MicroSkillResult(Guid.CreateVersion7(), 4m, "ok")],
                "Retry"));

            Assert.True(await consumer.Consumed.Any<EvidenceCreated>());
            await fixture.WaitForAsync(() => Task.FromResult(Volatile.Read(ref attempts) >= 2),
                TimeSpan.FromSeconds(10));
            Assert.True(attempts >= 2);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PermanentConsumerFailure_EndsInErrorQueue()
    {
        // In-memory transport: after UseMessageRetry is exhausted MassTransit publishes Fault<T>
        // (on RabbitMQ the same path moves the message to the `{queue}_error` queue).
        var attempts = 0;
        var harness = new InMemoryTestHarness();
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.UseMessageRetry(r =>
                r.Intervals(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20)));
        };

        var consumer = harness.Consumer(() =>
            new DelegateEvidenceConsumer(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new InvalidOperationException("Permanent failure");
            }));

        await harness.Start();
        try
        {
            var eventId = Guid.CreateVersion7();
            await harness.InputQueueSendEndpoint.Send(new EvidenceCreated(
                eventId,
                eventId,
                DateTimeOffset.UtcNow,
                DefaultTenant.Id,
                EvidenceCreated.CurrentVersion,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.NewGuid().ToString(),
                Guid.CreateVersion7(),
                Guid.NewGuid().ToString(),
                DateTimeOffset.UtcNow,
                [new MicroSkillResult(Guid.CreateVersion7(), 1m, "fail")],
                "Permanent"));

            await fixture.WaitForAsync(() => Task.FromResult(Volatile.Read(ref attempts) >= 3),
                TimeSpan.FromSeconds(10));
            Assert.True(await consumer.Consumed.Any<EvidenceCreated>());
            Assert.True(await harness.Published.Any<Fault<EvidenceCreated>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task EvidenceBeforeProfile_SucceedsAfterDelayedRedelivery()
    {
        var organisationId = DefaultTenant.Id;
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var classId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();

        // Publish evidence while the student learning profile does not exist yet.
        var evidenceId = await fixture.ApproveEvidenceAsync(
            organisationId,
            teacherId,
            studentId,
            classId,
            assessmentId,
            submissionId,
            "Redelivery race",
            [(microSkillId, 4m, "ok")]);

        Assert.Equal(0, await fixture.ScalarAsync("we_learning_it",
            $"select count(*) from profile_evidence_entries where id = '{evidenceId}'"));

        // Profile appears after the first consume attempts fail and schedule delayed redelivery.
        await Task.Delay(TimeSpan.FromSeconds(3));
        await fixture.SeedStudentProfileAsync(organisationId, studentId);

        await fixture.WaitForAsync(async () =>
            await fixture.ScalarAsync("we_learning_it",
                $"select count(*) from profile_evidence_entries where id = '{evidenceId}'") == 1,
            TimeSpan.FromSeconds(45));
    }

    private static ServiceProvider BuildPublishOnlyBus(FlowFixture fixture)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddMassTransit(bus =>
        {
            bus.UsingRabbitMq((_, cfg) =>
            {
                cfg.Host(fixture.RabbitHost, (ushort)fixture.RabbitPort, "/", h =>
                {
                    h.Username(fixture.RabbitUsername);
                    h.Password(fixture.RabbitPassword);
                });
                cfg.MessageTopology.SetEntityNameFormatter(new PlatformEventEntityNameFormatter());
                cfg.Publish<EvidenceCreated>(p => p.ExchangeType = ExchangeType.Topic);
            });
        });
        return services.BuildServiceProvider(true);
    }

    private sealed class DelegateEvidenceConsumer(Func<EvidenceCreated, Task> handler) : IConsumer<EvidenceCreated>
    {
        public Task Consume(ConsumeContext<EvidenceCreated> context) => handler(context.Message);
    }

    private sealed class PlatformEventEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => "we.platform.events";
    }
}
