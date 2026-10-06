using System.Net;
using System.Net.Http.Json;
using MasteryService.Application;
using WePlatform.Events;

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
    public async Task CrossSchool_PersonResource_BothDirections_Denied_AndConsumerRowsStayIsolated()
    {
        // Probe: mastery-service:person-resource
        // Consumer: consumers:evidence-created-mastery
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        var microSkillA = Guid.CreateVersion7();
        var microSkillB = Guid.CreateVersion7();
        _accessChecker.Allow(teacherA, studentA);
        _accessChecker.Allow(teacherB, studentB);
        _accessChecker.Allow(teacherA, studentB);
        _accessChecker.Allow(teacherB, studentA);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentA, schoolA, microSkillA, 4m));
        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentB, schoolB, microSkillB, 5m));

        await AssertCrossReadDeniedOrEmptyAsync(
            $"/api/v1/mastery/students/{studentA}",
            teacherB,
            schoolB);
        await AssertCrossReadDeniedOrEmptyAsync(
            $"/api/v1/mastery/students/{studentB}",
            teacherA,
            schoolA);
        var parentA = Guid.NewGuid().ToString();
        var parentB = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentA, studentA);
        _accessChecker.AllowParent(parentB, studentB);
        await AssertParentSummaryCrossReadDeniedOrEmptyAsync(
            $"/api/v1/mastery/students/{studentA}/parent-summary",
            parentB,
            schoolB);
        await AssertParentSummaryCrossReadDeniedOrEmptyAsync(
            $"/api/v1/mastery/students/{studentB}/parent-summary",
            parentA,
            schoolA);

        using var ownRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentA}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var ownResponse = await _client.SendAsync(ownRequest);
        ownResponse.EnsureSuccessStatusCode();
        var own = await ownResponse.Content.ReadFromJsonAsync<StudentMasteryResponse>();
        Assert.NotEmpty(own!.Records);
    }

    [Fact]
    public async Task SchoolLeader_CanReadOwnSchoolStudent_OtherSchoolParentAndStudentDenied()
    {
        // Probe: mastery-service:person-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var leaderA = Guid.NewGuid().ToString();
        var leaderB = Guid.NewGuid().ToString();
        var parentA = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var otherStudent = Guid.NewGuid().ToString();
        var microSkillA = Guid.CreateVersion7();
        _accessChecker.AllowLeader(leaderA, studentA);
        _accessChecker.AllowLeader(leaderB, studentA);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentA, schoolA, microSkillA, 4m));

        using var own = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentA}",
            leaderA,
            schoolA,
            TestJwt.SchoolLeaderRole);
        var ownResponse = await _client.SendAsync(own);
        ownResponse.EnsureSuccessStatusCode();
        var ownBody = await ownResponse.Content.ReadFromJsonAsync<StudentMasteryResponse>();
        Assert.NotEmpty(ownBody!.Records);

        using var otherSchool = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentA}",
            leaderB,
            schoolB,
            TestJwt.SchoolLeaderRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(otherSchool)).StatusCode);

        using var parent = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentA}",
            parentA,
            schoolA,
            TestJwt.ParentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(parent)).StatusCode);

        using var student = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/mastery/students/{studentA}",
            otherStudent,
            schoolA,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(student)).StatusCode);
    }

    private async Task AssertParentSummaryCrossReadDeniedOrEmptyAsync(string path, string callerId, Guid callerTenant)
    {
        using var request = TestJwt.Authorized(HttpMethod.Get, path, callerId, callerTenant, TestJwt.ParentRole);
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.OK,
            $"GET {path} returned {response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var payload = await response.Content.ReadFromJsonAsync<ParentMasterySummaryResponse>();
            Assert.Empty(payload!.Records);
        }
    }

    private async Task AssertCrossReadDeniedOrEmptyAsync(string path, string callerId, Guid callerTenant)
    {
        using var request = TestJwt.Authorized(HttpMethod.Get, path, callerId, callerTenant, TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.OK,
            $"GET {path} returned {response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            var payload = await response.Content.ReadFromJsonAsync<StudentMasteryResponse>();
            Assert.True(payload!.Records.Count == 0, body);
        }
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
            organisationId,
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
