using System.Net;
using System.Net.Http.Json;
using LearningGapService.Application;
using LearningGapService.Infrastructure.Messaging.Consumers;
using MassTransit;
using MassTransit.Testing;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace LearningGapService.Tests;

public class GapEndpointTests : IClassFixture<GapWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly IGapProcessor _processor;

    public GapEndpointTests(GapWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _processor = factory.GetProcessor();
    }

    [Fact]
    public async Task Teacher_CanViewStudentGapsAfterEvidenceProcessed()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherId, studentId);

        var evidence = CreateEvidence(studentId);
        await _processor.ProcessEvidenceCreatedAsync(evidence);

        var response = await SendAsAsync<StudentLearningGapsResponse>(
            HttpMethod.Get,
            $"/api/v1/gaps/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal(studentId, response.StudentUserId);
        Assert.Single(response.Gaps);
        var gap = response.Gaps[0];
        Assert.Equal("High", gap.Severity);
        Assert.Equal("High", gap.Urgency);
        Assert.Equal("Struggling", gap.ActualMastery);
        Assert.Contains("Expected mastery: Mastered", gap.Explanation);
        Assert.DoesNotContain(evidence.EvidenceId.ToString(), gap.Explanation);
        Assert.DoesNotContain(evidence.MicroSkillMarks[1].MicroSkillId.ToString(), gap.Explanation);
    }

    [Fact]
    public async Task Teacher_CannotViewGapsForUnassignedStudent()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/gaps/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotViewGapsEndpoint()
    {
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/gaps/students/{studentId}",
            studentId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProcessingSameEvidenceTwice_DoesNotDuplicateGaps()
    {
        var studentId = Guid.NewGuid().ToString();
        var evidence = CreateEvidence(studentId);

        await _processor.ProcessEvidenceCreatedAsync(evidence);
        await _processor.ProcessEvidenceCreatedAsync(evidence);

        var teacherId = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherId, studentId);

        var response = await SendAsAsync<StudentLearningGapsResponse>(
            HttpMethod.Get,
            $"/api/v1/gaps/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Single(response.Gaps);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role)
    {
        using var request = TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Missing response payload.");
    }

    private static EvidenceCreated CreateEvidence(string studentUserId)
    {
        var eventId = Guid.CreateVersion7();
        return new EvidenceCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            DefaultTenant.Id,
            EvidenceCreated.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            studentUserId,
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [
                new MicroSkillResult(Guid.CreateVersion7(), 5, "Excellent mastery."),
                new MicroSkillResult(Guid.CreateVersion7(), 1, "Needs support.")
            ]);
    }
}

public class EvidenceCreatedConsumerTests
{
    [Fact]
    public async Task EvidenceCreatedConsumer_PersistsGaps()
    {
        var harness = new InMemoryTestHarness();
        var processor = new InMemoryGapProcessor();
        var consumerHarness = harness.Consumer(() =>
            new EvidenceCreatedConsumer(
                processor,
                new WePlatform.Tenancy.TenantContext(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvidenceCreatedConsumer>.Instance));

        await harness.Start();
        try
        {
            var domainEvent = CreateEvidenceCreated();
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<EvidenceCreated>());
            Assert.Equal(1, processor.ProcessedCount);
            Assert.Equal(domainEvent.EvidenceId, processor.LastEvidenceId);
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
            DefaultTenant.Id,
            EvidenceCreated.CurrentVersion,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new MicroSkillResult(microSkillId, 2, "Partial understanding.")]);
    }

    private sealed class InMemoryGapProcessor : IGapProcessor
    {
        public int ProcessedCount { get; private set; }
        public Guid LastEvidenceId { get; private set; }

        public Task ProcessEvidenceCreatedAsync(EvidenceCreated evidence, CancellationToken cancellationToken = default)
        {
            ProcessedCount++;
            LastEvidenceId = evidence.EvidenceId;
            return Task.CompletedTask;
        }
    }
}
