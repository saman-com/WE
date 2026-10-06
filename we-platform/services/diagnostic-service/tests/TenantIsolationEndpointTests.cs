using System.Net;
using System.Net.Http.Json;
using DiagnosticService.Application;
using WePlatform.Events;

namespace DiagnosticService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<DiagnosticWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly IDiagnosticProcessor _processor;

    public TenantIsolationEndpointTests(DiagnosticWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _processor = factory.GetProcessor();
    }

    [Fact]
    public async Task CrossSchool_Diagnostics_BothDirections_Denied_AndConsumerRowsStayIsolated()
    {
        // Probe: diagnostic-service:person-resource
        // Consumer: consumers:evidence-created-diagnostic
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        _accessChecker.Allow(teacherA, studentA);
        _accessChecker.Allow(teacherB, studentB);
        // Even if access checker is overly permissive, tenant scope must hold:
        _accessChecker.Allow(teacherA, studentB);
        _accessChecker.Allow(teacherB, studentA);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentA, schoolA));
        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentB, schoolB));

        using var bReadsA = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentA}",
            teacherB,
            schoolB,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(bReadsA)).StatusCode);

        using var aReadsB = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentB}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(aReadsB)).StatusCode);

        using var aReadsA = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentA}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var ok = await _client.SendAsync(aReadsA);
        ok.EnsureSuccessStatusCode();
        var own = await ok.Content.ReadFromJsonAsync<StudentDiagnosticsResponse>();
        Assert.NotEmpty(own!.Diagnostics);
    }

    [Fact]
    public async Task SchoolLeader_CanReadOwnSchoolStudent_OtherSchoolParentAndStudentDenied()
    {
        // Probe: diagnostic-service:person-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var leaderA = Guid.NewGuid().ToString();
        var leaderB = Guid.NewGuid().ToString();
        var parentA = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        _accessChecker.AllowLeader(leaderA, studentA);
        _accessChecker.AllowLeader(leaderB, studentA);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentA, schoolA));

        using var own = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentA}",
            leaderA,
            schoolA,
            TestJwt.SchoolLeaderRole);
        var ownResponse = await _client.SendAsync(own);
        ownResponse.EnsureSuccessStatusCode();
        var ownBody = await ownResponse.Content.ReadFromJsonAsync<StudentDiagnosticsResponse>();
        Assert.NotEmpty(ownBody!.Diagnostics);

        using var otherSchool = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentA}",
            leaderB,
            schoolB,
            TestJwt.SchoolLeaderRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(otherSchool)).StatusCode);

        using var parent = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentA}",
            parentA,
            schoolA,
            TestJwt.ParentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(parent)).StatusCode);

        using var student = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/diagnostics/students/{studentA}",
            studentA,
            schoolA,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(student)).StatusCode);
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
