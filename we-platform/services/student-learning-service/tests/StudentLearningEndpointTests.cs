using System.Net;
using System.Net.Http.Json;
using StudentLearningService.Application;

namespace StudentLearningService.Tests;

public class StudentLearningEndpointTests : IClassFixture<StudentLearningWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public StudentLearningEndpointTests(StudentLearningWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/students/student-1/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_SyncEnrollment_CreatesProfile()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var profile = await SyncEnrollmentAsync(
            adminId,
            studentId,
            organisationId,
            classId,
            "7A",
            "7A");

        Assert.Equal(studentId, profile.StudentUserId);
        Assert.Single(profile.Enrollments);
        Assert.Equal(classId, profile.Enrollments[0].ClassId);
        Assert.Empty(profile.EvidenceTimeline);
    }

    [Fact]
    public async Task Admin_SyncEnrollment_IsIdempotentForSameClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        await SyncEnrollmentAsync(adminId, studentId, organisationId, classId, "7A", "7A");
        var profile = await SyncEnrollmentAsync(adminId, studentId, organisationId, classId, "7A", "7A");

        Assert.Single(profile.Enrollments);
    }

    [Fact]
    public async Task Admin_SyncEnrollment_AddsSecondClassToExistingProfile()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classA = Guid.NewGuid();
        var classB = Guid.NewGuid();

        await SyncEnrollmentAsync(adminId, studentId, organisationId, classA, "7A", "7A");
        var profile = await SyncEnrollmentAsync(adminId, studentId, organisationId, classB, "7B", "7B");

        Assert.Equal(2, profile.Enrollments.Count);
    }

    [Fact]
    public async Task Student_CanViewOwnProfile()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        await SyncEnrollmentAsync(adminId, studentId, Guid.NewGuid(), Guid.NewGuid(), "7A", "7A");

        var profile = await SendAsAsync<StudentProfileResponse>(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile",
            studentId,
            TestJwt.StudentRole);

        Assert.Equal(studentId, profile.StudentUserId);
    }

    [Fact]
    public async Task Student_CannotViewOtherProfile()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();
        await SyncEnrollmentAsync(adminId, studentId, Guid.NewGuid(), Guid.NewGuid(), "7A", "7A");

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile",
            otherStudentId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CanViewStudentInClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        await SyncEnrollmentAsync(adminId, studentId, Guid.NewGuid(), Guid.NewGuid(), "7A", "7A");
        _accessChecker.Allow(teacherId, studentId);

        var profile = await SendAsAsync<StudentProfileResponse>(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal(studentId, profile.StudentUserId);
    }

    [Fact]
    public async Task Teacher_CannotViewStudentOutsideClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        await SyncEnrollmentAsync(adminId, studentId, Guid.NewGuid(), Guid.NewGuid(), "7A", "7A");

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile",
            teacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_RecordEvidence_AppearsOnSlpTimeline()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var evidenceId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        await SyncEnrollmentAsync(adminId, studentId, Guid.NewGuid(), Guid.NewGuid(), "7A", "7A");
        _accessChecker.Allow(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/students/{studentId}/profile/evidence",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new RecordProfileEvidenceRequest(
            evidenceId,
            assessmentId,
            [microSkillId],
            "Quiz 1 evidence",
            DateTimeOffset.UtcNow));

        var recordResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

        var profile = await SendAsAsync<StudentProfileResponse>(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Single(profile.EvidenceTimeline);
        Assert.Equal(evidenceId, profile.EvidenceTimeline[0].Id);
        Assert.Equal("Quiz 1 evidence", profile.EvidenceTimeline[0].Title);
    }

    [Fact]
    public async Task Teacher_CanViewStudentProfileSummary()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var evidenceId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var recordedAt = DateTimeOffset.UtcNow;
        await SyncEnrollmentAsync(adminId, studentId, Guid.NewGuid(), Guid.NewGuid(), "7A", "7A");
        _accessChecker.Allow(teacherId, studentId);

        using var recordRequest = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/students/{studentId}/profile/evidence",
            teacherId,
            TestJwt.TeacherRole);
        recordRequest.Content = JsonContent.Create(new RecordProfileEvidenceRequest(
            evidenceId,
            assessmentId,
            [microSkillId],
            "Quiz 1 evidence",
            recordedAt));
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(recordRequest)).StatusCode);

        var summary = await SendAsAsync<StudentProfileSummaryResponse>(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile/summary",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal(studentId, summary.StudentUserId);
        Assert.Equal(1, summary.EvidenceCount);
        Assert.NotNull(summary.LatestActivityAt);
    }

    [Fact]
    public async Task Teacher_CannotViewProfileSummaryOutsideClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        await SyncEnrollmentAsync(adminId, studentId, Guid.NewGuid(), Guid.NewGuid(), "7A", "7A");

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile/summary",
            teacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ReturnsNotFoundWhenMissing()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile",
            adminId,
            TestJwt.AdminRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<StudentProfileResponse> SyncEnrollmentAsync(
        string adminId,
        string studentId,
        Guid organisationId,
        Guid classId,
        string className,
        string classCode)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/students/{studentId}/profile/enrollments",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new SyncProfileEnrollmentRequest(
            organisationId,
            classId,
            className,
            classCode));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<StudentProfileResponse>()
            ?? throw new InvalidOperationException("Missing profile payload.");
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role)
    {
        using var request = TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Missing response payload.");
    }
}
