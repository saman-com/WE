using System.Net;
using System.Net.Http.Json;
using AssessmentService.Application;
using AssessmentService.Domain;

using WePlatform.Tenancy;

namespace AssessmentService.Tests;

public class SubmissionEndpointTests : IClassFixture<AssessmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;

    public SubmissionEndpointTests(AssessmentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task Student_CanSubmitPublishedAssessment_BeforeDueDate()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Quiz 1",
            DateTimeOffset.UtcNow.AddDays(2));

        var submission = await SubmitAssessmentAsync(
            studentId,
            published.Id,
            organisationId,
            "My answers for quiz 1.");

        Assert.Equal(studentId, submission.StudentUserId);
        Assert.Equal(published.Id, submission.AssessmentId);
        Assert.Equal(SubmissionStatuses.Submitted, submission.Status);
        Assert.Equal("My answers for quiz 1.", submission.Responses);
        Assert.False(submission.IsLate);
        Assert.True(submission.SubmittedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Student_LateSubmission_IsAcceptedAndMarkedLate()
    {
        // Documented product behaviour: late submissions are accepted and flagged IsLate=true
        // (not rejected). Teachers can review late work separately; students cannot edit after submit.
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Past due quiz",
            DateTimeOffset.UtcNow.AddHours(-1));

        var submission = await SubmitAssessmentAsync(
            studentId,
            published.Id,
            organisationId,
            "Late but submitted.");

        Assert.Equal(SubmissionStatuses.Submitted, submission.Status);
        Assert.True(submission.IsLate);
        Assert.Equal("Late but submitted.", submission.Responses);
    }

    [Fact]
    public async Task Student_CannotSubmitTwice()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "One attempt only",
            null);

        await SubmitAssessmentAsync(studentId, published.Id, organisationId, "First attempt.");

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{published.Id}/submissions",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new SubmitAssessmentRequest("Second attempt."));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotAccessOtherStudentsSubmission()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);
        _accessChecker.AllowStudent(otherStudentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Shared class quiz",
            null);

        var submission = await SubmitAssessmentAsync(studentId, published.Id, organisationId, "Student one answers.");

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}/submissions/{submission.Id}",
            otherStudentId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewOwnSubmissionViaMeEndpoint()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Profile quiz",
            null);

        var submitted = await SubmitAssessmentAsync(studentId, published.Id, organisationId, "My work.");

        var loaded = await SendAsAsync<SubmissionResponse>(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}/submissions/me",
            studentId,
            TestJwt.StudentRole,
            organisationId);

        Assert.Equal(submitted.Id, loaded.Id);
        Assert.Equal("My work.", loaded.Responses);
    }

    [Fact]
    public async Task Teacher_CanListSubmissionsForReview()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Review quiz",
            null);
        var submitted = await SubmitAssessmentAsync(studentId, published.Id, organisationId, "Student work.");

        var listed = await SendAsAsync<List<SubmissionResponse>>(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}/submissions",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

        Assert.Single(listed);
        Assert.Equal(submitted.Id, listed[0].Id);
        Assert.Equal("Student work.", listed[0].Responses);
    }

    [Fact]
    public async Task Student_CannotListAllSubmissionsForReview()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var published = await PublishAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Private submissions",
            null);
        await SubmitAssessmentAsync(studentId, published.Id, organisationId, "Student work.");

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}/submissions",
            studentId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotSubmitToDraftAssessment()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var draft = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Hidden draft",
            null);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{draft.Id}/submissions",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new SubmitAssessmentRequest("Should fail."));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<AssessmentResponse> CreateAssessmentAsync(
        string teacherId,
        Guid organisationId,
        Guid classId,
        string title,
        DateTimeOffset? dueAt)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/assessments",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateAssessmentRequest(
            organisationId,
            classId,
            title,
            null,
            dueAt,
            [],
            [Guid.NewGuid()]));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AssessmentResponse>()
            ?? throw new InvalidOperationException("Missing assessment payload.");
    }

    private async Task<AssessmentResponse> PublishAssessmentAsync(
        string teacherId,
        Guid organisationId,
        Guid classId,
        string title,
        DateTimeOffset? dueAt)
    {
        var draft = await CreateAssessmentAsync(teacherId, organisationId, classId, title, dueAt);
        return await SendAsAsync<AssessmentResponse>(
            HttpMethod.Post,
            $"/api/v1/assessments/{draft.Id}/publish",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);
    }

    private async Task<SubmissionResponse> SubmitAssessmentAsync(
        string studentId,
        Guid assessmentId,
        Guid organisationId,
        string responses)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{assessmentId}/submissions",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new SubmitAssessmentRequest(responses));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("Missing submission payload.");
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role, Guid? tenantId = null)
    {
        using var request = tenantId.HasValue
            ? TestJwt.Authorized(method, url, userId, tenantId.Value, role)
            : TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Missing response payload.");
    }
}
