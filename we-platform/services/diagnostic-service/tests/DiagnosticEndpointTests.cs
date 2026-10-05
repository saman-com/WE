using System.Net;
using System.Net.Http.Json;
using DiagnosticService.Application;
using DiagnosticService.Infrastructure.Messaging.Consumers;
using MassTransit;
using MassTransit.Testing;
using WePlatform.Events;

namespace DiagnosticService.Tests;

public class DiagnosticEndpointTests : IClassFixture<DiagnosticWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly IDiagnosticProcessor _processor;

    public DiagnosticEndpointTests(DiagnosticWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _processor = factory.GetProcessor();
    }

    [Fact]
    public async Task Teacher_CanViewStudentDiagnosticsAfterEvidenceProcessed()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherId, studentId);

        var evidence = CreateEvidence(studentId);
        await _processor.ProcessEvidenceCreatedAsync(evidence);

        var response = await SendAsAsync<StudentDiagnosticsResponse>(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal(studentId, response.StudentUserId);
        Assert.Equal(2, response.Diagnostics.Count);
        Assert.Contains(response.Diagnostics, item => item.Status == "Mastered");
        Assert.Contains(response.Diagnostics, item => item.Status == "Struggling");
        Assert.All(response.Diagnostics, item =>
        {
            Assert.Contains(evidence.EvidenceId.ToString(), item.Reason);
            Assert.Contains(item.MicroSkillId.ToString(), item.Reason);
        });
    }

    [Fact]
    public async Task Teacher_CannotViewDiagnosticsForUnassignedStudent()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotViewDiagnosticsEndpoint()
    {
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentId}",
            studentId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProcessingSameEvidenceTwice_DoesNotDuplicateDiagnostics()
    {
        var studentId = Guid.NewGuid().ToString();
        var evidence = CreateEvidence(studentId);

        await _processor.ProcessEvidenceCreatedAsync(evidence);
        await _processor.ProcessEvidenceCreatedAsync(evidence);

        var teacherId = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherId, studentId);

        var response = await SendAsAsync<StudentDiagnosticsResponse>(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal(2, response.Diagnostics.Count);
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
            Guid.CreateVersion7(),
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
    public async Task EvidenceCreatedConsumer_PersistsDiagnostics()
    {
        var harness = new InMemoryTestHarness();
        var processor = new InMemoryDiagnosticProcessor();
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

    private sealed class InMemoryDiagnosticProcessor : IDiagnosticProcessor
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
