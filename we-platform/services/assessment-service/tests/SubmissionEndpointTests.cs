using System.Net;
using System.Net.Http.Json;
using AssessmentService.Application;
using AssessmentService.Domain;

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
            "Late but submitted.");

        Assert.Equal(SubmissionStatuses.Submitted, submission.Status);
        Assert.True(submission.IsLate);
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

        await SubmitAssessmentAsync(studentId, published.Id, "First attempt.");

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{published.Id}/submissions",
            studentId,
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

        var submission = await SubmitAssessmentAsync(studentId, published.Id, "Student one answers.");

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

        var submitted = await SubmitAssessmentAsync(studentId, published.Id, "My work.");

        var loaded = await SendAsAsync<SubmissionResponse>(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}/submissions/me",
            studentId,
            TestJwt.StudentRole);

        Assert.Equal(submitted.Id, loaded.Id);
        Assert.Equal("My work.", loaded.Responses);
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
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateAssessmentRequest(
            organisationId,
            classId,
            title,
            null,
            dueAt,
            [],
            []));

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
            TestJwt.TeacherRole);
    }

    private async Task<SubmissionResponse> SubmitAssessmentAsync(
        string studentId,
        Guid assessmentId,
        string responses)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{assessmentId}/submissions",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new SubmitAssessmentRequest(responses));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<SubmissionResponse>()
            ?? throw new InvalidOperationException("Missing submission payload.");
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
