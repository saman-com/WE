using System.Net;
using System.Net.Http.Json;
using MasteryService.Application;
using MasteryService.Domain;
using MasteryService.Infrastructure.Messaging.Consumers;
using MassTransit;
using MassTransit.Testing;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace MasteryService.Tests;

public class MasteryEndpointTests : IClassFixture<MasteryWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly IMasteryProcessor _processor;

    public MasteryEndpointTests(MasteryWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _processor = factory.GetProcessor();
    }

    [Fact]
    public async Task Teacher_CanViewStudentMasteryAfterMultipleEvidenceProcessed()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.Allow(teacherId, studentId);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentId, microSkillId, 4m));
        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentId, microSkillId, 5m));

        var response = await SendAsAsync<StudentMasteryResponse>(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal(studentId, response.StudentUserId);
        Assert.Single(response.Records);
        var record = response.Records[0];
        Assert.Equal(microSkillId, record.MicroSkillId);
        Assert.Equal(MasteryLevel.Mastered, record.MasteryLevel);
        Assert.Equal(2, record.EvidenceCount);
        Assert.Equal(4.5m, record.WeightedAverage);
    }

    [Fact]
    public async Task Teacher_CannotViewMasteryForUnassignedStudent()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewOwnMasteryEndpoint()
    {
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}",
            studentId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotViewOtherStudentMasteryEndpoint()
    {
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{otherStudentId}",
            studentId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProcessingSameEvidenceTwice_DoesNotDuplicateMarks()
    {
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        var evidence = CreateEvidence(studentId, microSkillId, 5m);

        await _processor.ProcessEvidenceCreatedAsync(evidence);
        await _processor.ProcessEvidenceCreatedAsync(evidence);

        var teacherId = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherId, studentId);

        var response = await SendAsAsync<StudentMasteryResponse>(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Single(response.Records);
        Assert.Equal(1, response.Records[0].EvidenceCount);
        Assert.Equal(MasteryLevel.Proficient, response.Records[0].MasteryLevel);
    }

    [Fact]
    public async Task NewEvidence_RecalculatesMasteryLevel()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.Allow(teacherId, studentId);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentId, microSkillId, 2m));

        var developing = await SendAsAsync<StudentMasteryResponse>(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);
        Assert.Equal(MasteryLevel.Developing, developing.Records[0].MasteryLevel);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentId, microSkillId, 5m));

        var proficient = await SendAsAsync<StudentMasteryResponse>(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}",
            teacherId,
            TestJwt.TeacherRole);
        Assert.Equal(MasteryLevel.Proficient, proficient.Records[0].MasteryLevel);
        Assert.Equal(3.5m, proficient.Records[0].WeightedAverage);
    }

    [Fact]
    public async Task Parent_CanViewLinkedChildMasterySummary()
    {
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.Allow(teacherId, studentId);
        _accessChecker.AllowParent(parentId, studentId);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentId, microSkillId, 4m));

        var response = await SendAsAsync<ParentMasterySummaryResponse>(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}/parent-summary",
            parentId,
            TestJwt.ParentRole);

        Assert.Equal(studentId, response.StudentUserId);
        var record = Assert.Single(response.Records);
        Assert.Equal(microSkillId, record.MicroSkillId);
        Assert.Equal(MasteryLevel.Proficient, record.MasteryLevel);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role)
    {
        using var request = TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Missing response payload.");
    }

    private static EvidenceCreated CreateEvidence(string studentUserId, Guid microSkillId, decimal mark)
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
            [new MicroSkillResult(microSkillId, mark, "Teacher feedback.")]);
    }
}

public class EvidenceCreatedConsumerTests
{
    [Fact]
    public async Task EvidenceCreatedConsumer_InvokesProcessor()
    {
        var harness = new InMemoryTestHarness();
        var processor = new InMemoryMasteryProcessor();
        var consumerHarness = harness.Consumer(() =>
            new EvidenceCreatedConsumer(
                processor,
                new TenantContext(),
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
            [new MicroSkillResult(microSkillId, 3m, "Partial understanding.")]);
    }

    private sealed class InMemoryMasteryProcessor : IMasteryProcessor
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
