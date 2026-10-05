using System.Net;
using System.Net.Http.Json;
using AssessmentService.Application;
using AssessmentService.Domain;
using WePlatform.AspNetCore;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace AssessmentService.Tests;

public class AssessmentEndpointTests : IClassFixture<AssessmentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;
    private readonly FakeDomainEventPublisher _eventPublisher;

    public AssessmentEndpointTests(AssessmentWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _eventPublisher = factory.EventPublisher;
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/assessments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CanCreateDraftAssessment_LinkedToClassAndCurriculumIds()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var learningObjectiveId = Guid.NewGuid();
        var microSkillId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var assessment = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Unit 3 Quiz",
            "Complete all questions.",
            DateTimeOffset.UtcNow.AddDays(7),
            [learningObjectiveId],
            [microSkillId]);

        Assert.Equal("Unit 3 Quiz", assessment.Title);
        Assert.Equal(AssessmentStatuses.Draft, assessment.Status);
        Assert.Equal(classId, assessment.ClassId);
        Assert.Equal(organisationId, assessment.OrganisationId);
        Assert.Contains(learningObjectiveId, assessment.LearningObjectiveIds);
        Assert.Contains(microSkillId, assessment.MicroSkillIds);
        Assert.Null(assessment.PublishedAt);
    }

    [Fact]
    public async Task Teacher_CanPublishAssessment_DraftBecomesPublished()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var draft = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Midterm",
            null,
            null,
            [],
            []);

        var published = await SendAsAsync<AssessmentResponse>(
            HttpMethod.Post,
            $"/api/v1/assessments/{draft.Id}/publish",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

        Assert.Equal(AssessmentStatuses.Published, published.Status);
        Assert.NotNull(published.PublishedAt);
    }

    [Fact]
    public async Task PublishAssessment_PublishesAssessmentPublishedEventForClassStudents()
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
            "Weekly quiz",
            null,
            null,
            [],
            []);

        await SendAsAsync<AssessmentResponse>(
            HttpMethod.Post,
            $"/api/v1/assessments/{draft.Id}/publish",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

        var publishedEvent = _eventPublisher.AssessmentPublishedEvents.Single(e => e.AssessmentId == draft.Id);
        Assert.Equal("Weekly quiz", publishedEvent.Title);
        Assert.Equal(studentId, Assert.Single(publishedEvent.RecipientUserIds));
        Assert.Equal(AssessmentPublished.CurrentVersion, publishedEvent.Version);
    }

    [Fact]
    public async Task Student_CannotCreateAssessment()
    {
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/assessments",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new CreateAssessmentRequest(
            organisationId,
            classId,
            "Student attempt",
            null,
            null,
            [],
            []));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CreateAssessment_WithEmptyTitle_ReturnsTranslatableErrorCode()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/assessments",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateAssessmentRequest(
            organisationId,
            classId,
            "   ",
            null,
            null,
            [],
            []));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("validation.invalid_request", error.Code);
    }

    [Fact]
    public async Task Student_CanListPublishedAssessments_ForEnrolledClassOnly()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);
        _accessChecker.AllowStudent(otherStudentId, organisationId, classId);

        var draft = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Draft only",
            null,
            null,
            [],
            []);
        var published = await SendAsAsync<AssessmentResponse>(
            HttpMethod.Post,
            $"/api/v1/assessments/{draft.Id}/publish",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

        var visible = await SendAsAsync<PagedResponse<AssessmentResponse>>(
            HttpMethod.Get,
            $"/api/v1/assessments?organisationId={organisationId}&classId={classId}",
            studentId,
            TestJwt.StudentRole,
            organisationId);

        Assert.Single(visible.Items);
        Assert.Equal(published.Id, visible.Items[0].Id);
    }

    [Fact]
    public async Task Student_CannotSeeDraftAssessments()
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
            null,
            null,
            [],
            []);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/{draft.Id}",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_NotAssignedToClass_CannotCreateOrPublish()
    {
        var teacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(otherTeacherId, organisationId, classId);

        using var createRequest = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/assessments",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        createRequest.Content = JsonContent.Create(new CreateAssessmentRequest(
            organisationId,
            classId,
            "Unauthorized",
            null,
            null,
            [],
            []));
        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);

        var draft = await CreateAssessmentAsync(
            otherTeacherId,
            organisationId,
            classId,
            "Other teacher draft",
            null,
            null,
            [],
            []);

        using var publishRequest = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{draft.Id}/publish",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var publishResponse = await _client.SendAsync(publishRequest);
        Assert.Equal(HttpStatusCode.Forbidden, publishResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CanViewClassAssessmentSummary()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var draft = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Weekly Quiz",
            null,
            DateTimeOffset.UtcNow.AddDays(2),
            [],
            []);
        var published = await PublishAssessmentAsync(teacherId, draft.Id, organisationId);

        var summaries = await SendAsAsync<List<ClassAssessmentSummaryResponse>>(
            HttpMethod.Get,
            $"/api/v1/assessments/class-summary?organisationId={organisationId}&classId={classId}",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

        Assert.Contains(summaries, item => item.Id == published.Id && item.SubmissionCount == 0);
    }

    [Fact]
    public async Task SchoolLeader_CanViewClassAssessmentSummaryForTheirOrganisation()
    {
        var teacherId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var draft = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Leadership summary quiz",
            null,
            DateTimeOffset.UtcNow.AddDays(2),
            [],
            []);
        var published = await PublishAssessmentAsync(teacherId, draft.Id, organisationId);

        var summaries = await SendAsAsync<List<ClassAssessmentSummaryResponse>>(
            HttpMethod.Get,
            $"/api/v1/assessments/class-summary?organisationId={organisationId}&classId={classId}",
            leaderId,
            TestJwt.SchoolLeaderRole,
            organisationId);

        Assert.Contains(summaries, item => item.Id == published.Id);

        using var otherTenant = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/class-summary?organisationId={organisationId}&classId={classId}",
            leaderId,
            Guid.NewGuid(),
            TestJwt.SchoolLeaderRole);
        var denied = await _client.SendAsync(otherTenant);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotViewClassAssessmentSummaryForUnassignedClass()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/class-summary?organisationId={organisationId}&classId={classId}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewAssessmentSummaryWithPendingAndCompleted()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var learningObjectiveId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _accessChecker.AllowStudent(studentId, organisationId, classId);

        var pending = await PublishAssessmentAsync(
            teacherId,
            (await CreateAssessmentAsync(
                teacherId,
                organisationId,
                classId,
                "Pending quiz",
                null,
                DateTimeOffset.UtcNow.AddDays(3),
                [learningObjectiveId],
                [])).Id,
            organisationId);
        var completed = await PublishAssessmentAsync(
            teacherId,
            (await CreateAssessmentAsync(
                teacherId,
                organisationId,
                classId,
                "Completed quiz",
                null,
                DateTimeOffset.UtcNow.AddDays(1),
                [],
                [])).Id,
            organisationId);

        using var submitRequest = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/assessments/{completed.Id}/submissions",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        submitRequest.Content = JsonContent.Create(new SubmitAssessmentRequest("Done."));
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(submitRequest)).StatusCode);

        var summaries = await SendAsAsync<PagedResponse<StudentAssessmentSummaryResponse>>(
            HttpMethod.Get,
            $"/api/v1/assessments/student-summary?organisationId={organisationId}&classId={classId}",
            studentId,
            TestJwt.StudentRole,
            organisationId);

        Assert.Equal(2, summaries.Items.Count);
        var pendingSummary = Assert.Single(summaries.Items, item => item.Id == pending.Id);
        Assert.False(pendingSummary.HasSubmitted);
        Assert.Null(pendingSummary.SubmittedAt);
        Assert.Contains(learningObjectiveId, pendingSummary.LearningObjectiveIds);

        var completedSummary = Assert.Single(summaries.Items, item => item.Id == completed.Id);
        Assert.True(completedSummary.HasSubmitted);
        Assert.NotNull(completedSummary.SubmittedAt);
    }

    [Fact]
    public async Task Student_CannotViewAssessmentSummaryForUnenrolledClass()
    {
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/student-summary?organisationId={organisationId}&classId={classId}",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotViewStudentAssessmentSummary()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/assessments/student-summary?organisationId={organisationId}&classId={classId}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotEditPublishedAssessment()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var draft = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Locked after publish",
            null,
            DateTimeOffset.UtcNow.AddDays(3),
            [],
            []);
        var published = await PublishAssessmentAsync(teacherId, draft.Id, organisationId);

        using var request = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/assessments/{published.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new UpdateAssessmentRequest(
            "Attempted edit",
            "Should be rejected",
            DateTimeOffset.UtcNow.AddDays(5),
            [],
            []));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var unchanged = await SendAsAsync<AssessmentResponse>(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);
        Assert.Equal("Locked after publish", unchanged.Title);
        Assert.Equal(AssessmentStatuses.Published, unchanged.Status);
    }

    [Fact]
    public async Task Teacher_CannotDeletePublishedAssessment()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var draft = await CreateAssessmentAsync(
            teacherId,
            organisationId,
            classId,
            "Published stays",
            null,
            null,
            [],
            []);
        var published = await PublishAssessmentAsync(teacherId, draft.Id, organisationId);

        using var request = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/assessments/{published.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var stillPresent = await SendAsAsync<AssessmentResponse>(
            HttpMethod.Get,
            $"/api/v1/assessments/{published.Id}",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);
        Assert.Equal(published.Id, stillPresent.Id);
        Assert.Equal(AssessmentStatuses.Published, stillPresent.Status);
    }

    private async Task<AssessmentResponse> PublishAssessmentAsync(string teacherId, Guid assessmentId, Guid organisationId) =>
        await SendAsAsync<AssessmentResponse>(
            HttpMethod.Post,
            $"/api/v1/assessments/{assessmentId}/publish",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

    private async Task<AssessmentResponse> CreateAssessmentAsync(
        string teacherId,
        Guid organisationId,
        Guid classId,
        string title,
        string? instructions,
        DateTimeOffset? dueAt,
        IReadOnlyList<Guid> learningObjectiveIds,
        IReadOnlyList<Guid> microSkillIds)
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
            instructions,
            dueAt,
            learningObjectiveIds,
            microSkillIds));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<AssessmentResponse>()
            ?? throw new InvalidOperationException("Missing assessment payload.");
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
