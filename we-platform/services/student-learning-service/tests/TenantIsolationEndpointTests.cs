using System.Net;
using System.Net.Http.Json;
using StudentLearningService.Application;
using WePlatform.Events;

namespace StudentLearningService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<StudentLearningWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly IProfileEvidenceProcessor _processor;

    public TenantIsolationEndpointTests(StudentLearningWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _processor = factory.GetProcessor();
    }

    public static TheoryData<string, string> PersonResourceRoutes => new()
    {
        { "GET", "/api/v1/students/{studentUserId}/profile" },
        { "GET", "/api/v1/students/{studentUserId}/profile/summary" },
        { "POST", "/api/v1/students/{studentUserId}/profile/enrollments" },
        { "POST", "/api/v1/students/{studentUserId}/profile/evidence" }
    };

    [Theory]
    [MemberData(nameof(PersonResourceRoutes))]
    public async Task CrossSchool_PersonResource_BothDirections_Denied(string method, string template)
    {
        // Probe: student-learning-service:person-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        _accessChecker.Allow(teacherA, studentA);
        _accessChecker.Allow(teacherB, studentB);

        await SyncEnrollmentAsync(adminA, studentA, schoolA, classA, schoolA);
        await SyncEnrollmentAsync(adminB, studentB, schoolB, classB, schoolB);

        await AssertDeniedAsync(method, Expand(template, studentA), teacherB, schoolB, classA, schoolA);
        await AssertDeniedAsync(method, Expand(template, studentB), teacherA, schoolA, classB, schoolB);
    }

    [Fact]
    public async Task CrossSchool_EvidenceConsumer_BothDirections_ProfileStaysIsolated()
    {
        // Consumer: consumers:evidence-created-learning
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        var classA = Guid.CreateVersion7();
        var classB = Guid.CreateVersion7();
        _accessChecker.Allow(teacherA, studentA);
        _accessChecker.Allow(teacherB, studentB);

        await SyncEnrollmentAsync(adminA, studentA, schoolA, classA, schoolA);
        await SyncEnrollmentAsync(adminB, studentB, schoolB, classB, schoolB);

        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentA, schoolA));
        await _processor.ProcessEvidenceCreatedAsync(CreateEvidence(studentB, schoolB));

        using var readA = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentA}/profile",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var okA = await _client.SendAsync(readA);
        okA.EnsureSuccessStatusCode();
        var profileA = await okA.Content.ReadFromJsonAsync<StudentProfileResponse>();
        Assert.NotEmpty(profileA!.EvidenceTimeline);

        using var crossRead = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentB}/profile",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var crossResponse = await _client.SendAsync(crossRead);
        Assert.True(
            crossResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"cross profile read returned {crossResponse.StatusCode}");
    }

    private async Task AssertDeniedAsync(
        string method,
        string path,
        string callerId,
        Guid callerTenant,
        Guid classId,
        Guid organisationId)
    {
        using var request = TestJwt.Authorized(
            new HttpMethod(method),
            path,
            callerId,
            callerTenant,
            method == "POST" && path.Contains("/enrollments", StringComparison.Ordinal)
                ? TestJwt.AdminRole
                : TestJwt.TeacherRole);
        if (method == "POST")
        {
            request.Content = path.Contains("/enrollments", StringComparison.Ordinal)
                ? JsonContent.Create(new SyncProfileEnrollmentRequest(organisationId, classId, "7A", "7A"))
                : JsonContent.Create(new RecordProfileEvidenceRequest(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    [Guid.CreateVersion7()],
                    "Cross-school evidence",
                    DateTimeOffset.UtcNow));
        }

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest,
            $"{method} {path} returned {response.StatusCode}");
    }

    private static string Expand(string template, string studentUserId) =>
        template.Replace("{studentUserId}", studentUserId, StringComparison.Ordinal);

    private async Task SyncEnrollmentAsync(
        string adminId,
        string studentId,
        Guid organisationId,
        Guid classId,
        Guid tenantId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/students/{studentId}/profile/enrollments",
            adminId,
            tenantId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new SyncProfileEnrollmentRequest(
            organisationId,
            classId,
            "7A",
            "7A"));

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
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
            [new MicroSkillResult(Guid.CreateVersion7(), 4, "Strong work.")]);
    }
}
