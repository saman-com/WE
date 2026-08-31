using System.Net;
using MasteryService.Application;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace MasteryService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<MasteryWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly IMasteryProcessor _processor;

    public TenantIsolationEndpointTests(MasteryWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _processor = factory.GetProcessor();
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotViewStudentMastery()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.Allow(teacherId, studentId);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentId, tenantB, microSkillId, 4m));

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentId}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static EvidenceCreated CreateEvidence(
        string studentUserId,
        Guid organisationId,
        Guid microSkillId,
        decimal mark)
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
            organisationId,
            studentUserId,
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            DateTimeOffset.UtcNow,
            [new MicroSkillResult(microSkillId, mark, "Teacher feedback.")]);
    }
}
