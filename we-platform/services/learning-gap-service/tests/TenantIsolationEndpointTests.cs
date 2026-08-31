using System.Net;
using LearningGapService.Application;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace LearningGapService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<GapWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly IGapProcessor _processor;

    public TenantIsolationEndpointTests(GapWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _processor = factory.GetProcessor();
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotViewStudentGaps()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherId, studentId);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentId, tenantB));

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/gaps/students/{studentId}",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static EvidenceCreated CreateEvidence(string studentUserId, Guid organisationId)
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
            [
                new MicroSkillResult(Guid.CreateVersion7(), 5, "Excellent mastery."),
                new MicroSkillResult(Guid.CreateVersion7(), 1, "Needs support.")
            ]);
    }
}
