using System.Net;
using System.Net.Http.Json;
using LearningGapService.Application;
using WePlatform.Events;

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
    public async Task CrossSchool_PersonResource_BothDirections_Denied_AndConsumerRowsStayIsolated()
    {
        // Probe: learning-gap-service:person-resource
        // Consumer: consumers:evidence-created-gaps
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherA, studentA);
        _accessChecker.Allow(teacherB, studentB);
        _accessChecker.Allow(teacherA, studentB);
        _accessChecker.Allow(teacherB, studentA);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentA, schoolA));
        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentB, schoolB));

        await AssertCrossReadDeniedOrEmptyAsync(teacherB, schoolB, studentA);
        await AssertCrossReadDeniedOrEmptyAsync(teacherA, schoolA, studentB);

        using var ownRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/gaps/students/{studentA}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var ownResponse = await _client.SendAsync(ownRequest);
        ownResponse.EnsureSuccessStatusCode();
        var own = await ownResponse.Content.ReadFromJsonAsync<StudentLearningGapsResponse>();
        Assert.NotEmpty(own!.Gaps);
    }

    private async Task AssertCrossReadDeniedOrEmptyAsync(string callerId, Guid callerTenant, string studentUserId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/gaps/students/{studentUserId}",
            callerId,
            callerTenant,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.OK,
            $"GET gaps returned {response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content.ReadFromJsonAsync<StudentLearningGapsResponse>();
            Assert.Empty(payload!.Gaps);
        }
    }

    private static EvidenceCreated CreateEvidence(string studentUserId, Guid organisationId)
    {
        var eventId = Guid.CreateVersion7();
        return new EvidenceCreated(
            eventId,
            eventId,
            DateTimeOffset.UtcNow,
            organisationId,
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
