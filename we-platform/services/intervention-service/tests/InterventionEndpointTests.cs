using System.Net;
using System.Net.Http.Json;
using InterventionService.Application;
using InterventionService.Domain;
using WePlatform.Tenancy;

namespace InterventionService.Tests;

public class InterventionEndpointTests : IClassFixture<InterventionWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly RecordingInterventionEvents _events;

    public InterventionEndpointTests(InterventionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _events = factory.Events;
    }

    [Fact]
    public async Task Teacher_CreatesInterventionLinkedToStudentAndGap()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = DefaultTenant.Id;
        var learningGapId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var created = await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                organisationId,
                studentId,
                learningGapId,
                "Review conservation of mass with guided practice.",
                "Initial planning notes.",
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(14),
                DateTimeOffset.UtcNow.AddDays(7)));

        Assert.Equal(studentId, created.StudentUserId);
        Assert.Equal(learningGapId, created.LearningGapId);
        Assert.Equal(teacherId, created.AssignedTeacherUserId);
        Assert.Equal(InterventionStatuses.Planned, created.Status);
        Assert.Equal("Review conservation of mass with guided practice.", created.PlannedActions);
        Assert.Contains(_events.Published, item => item.Id == created.Id && item.Status == InterventionStatuses.Planned);
    }

    [Fact]
    public async Task CreateIntervention_RejectsStoredIdsInTheText()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateInterventionRequest(
            DefaultTenant.Id,
            studentId,
            Guid.CreateVersion7(),
            "Address the gap in micro-skill 00000000-0000-4000-8000-000000001004.",
            "Evidence 01a10b8d-70ab-7d9c-aebc-de762c4d9f47",
            null,
            null,
            null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("intervention.text_contains_id", body?["code"]);
    }

    [Fact]
    public async Task Teacher_ListsInterventionsForStudent()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = DefaultTenant.Id;
        var learningGapId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, studentId);

        await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                organisationId,
                studentId,
                learningGapId,
                "Targeted revision sessions.",
                null,
                null,
                null,
                null));

        var response = await SendAsAsync<StudentInterventionsResponse>(
            HttpMethod.Get,
            $"/api/v1/interventions?studentUserId={studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Equal(studentId, response.StudentUserId);
        Assert.Single(response.Interventions);
        Assert.Equal(learningGapId, response.Interventions[0].LearningGapId);
    }

    [Fact]
    public async Task Teacher_UpdatesInterventionNotes()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var created = await CreateSampleInterventionAsync(teacherId, studentId);

        var updated = await SendAsAsync<InterventionResponse>(
            HttpMethod.Patch,
            $"/api/v1/interventions/{created.Id}",
            teacherId,
            TestJwt.TeacherRole,
            new PatchInterventionRequest(null, "Updated progress notes.", null, null, null, null, null));

        Assert.Equal("Updated progress notes.", updated.Notes);
    }

    [Fact]
    public async Task Teacher_MarksInterventionComplete()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var created = await CreateSampleInterventionAsync(teacherId, studentId);

        var active = await SendAsAsync<InterventionResponse>(
            HttpMethod.Patch,
            $"/api/v1/interventions/{created.Id}",
            teacherId,
            TestJwt.TeacherRole,
            new PatchInterventionRequest(InterventionStatuses.Active, null, null, null, null, null, null));

        var completed = await SendAsAsync<InterventionResponse>(
            HttpMethod.Patch,
            $"/api/v1/interventions/{active.Id}",
            teacherId,
            TestJwt.TeacherRole,
            new PatchInterventionRequest(
                InterventionStatuses.Completed,
                null,
                "Student demonstrated improved understanding.",
                null,
                null,
                null,
                null));

        Assert.Equal(InterventionStatuses.Completed, completed.Status);
        Assert.Equal("Student demonstrated improved understanding.", completed.Outcome);
    }

    [Fact]
    public async Task StatusTransition_FollowsPlannedActiveCompletedClosedLifecycle()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var intervention = await CreateSampleInterventionAsync(teacherId, studentId);
        Assert.Equal(InterventionStatuses.Planned, intervention.Status);

        intervention = await PatchStatusAsync(intervention.Id, teacherId, DefaultTenant.Id, InterventionStatuses.Active);
        Assert.Equal(InterventionStatuses.Active, intervention.Status);

        intervention = await PatchStatusAsync(intervention.Id, teacherId, DefaultTenant.Id, InterventionStatuses.Completed);
        Assert.Equal(InterventionStatuses.Completed, intervention.Status);

        intervention = await PatchStatusAsync(intervention.Id, teacherId, DefaultTenant.Id, InterventionStatuses.Closed);
        Assert.Equal(InterventionStatuses.Closed, intervention.Status);
    }

    [Fact]
    public async Task StatusTransition_InvalidTransition_Returns422()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var intervention = await CreateSampleInterventionAsync(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/interventions/{intervention.Id}",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(
            new PatchInterventionRequest(InterventionStatuses.Closed, null, null, null, null, null, null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CannotModifyIntervention()
    {
        var teacherId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);
        _accessChecker.AllowSchoolLeader(leaderId, studentId);

        var created = await CreateSampleInterventionAsync(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/interventions/{created.Id}",
            leaderId,
            TestJwt.SchoolLeaderRole);
        request.Content = JsonContent.Create(
            new PatchInterventionRequest(null, "School leader review notes.", null, null, null, null, null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CanListOrganisationInterventions()
    {
        var teacherId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var learningGapId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, studentId);
        _accessChecker.AllowSchoolLeaderForOrganisation(leaderId, organisationId);

        var created = await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            organisationId,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                organisationId,
                studentId,
                learningGapId,
                "Guided practice for target gap.",
                null,
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(14),
                null));

        var response = await SendAsAsync<OrganisationInterventionsResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{organisationId}/interventions",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(organisationId, response.OrganisationId);
        var intervention = Assert.Single(response.Interventions);
        Assert.Equal(created.Id, intervention.Id);
        Assert.Equal(studentId, intervention.StudentUserId);
        Assert.Equal(learningGapId, intervention.LearningGapId);
        Assert.Equal(teacherId, intervention.AssignedTeacherUserId);
        Assert.Equal(InterventionStatuses.Planned, intervention.Status);
    }

    [Fact]
    public async Task SchoolLeader_CanFilterOrganisationInterventionsByStatus()
    {
        var teacherId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, studentId);
        _accessChecker.AllowSchoolLeaderForOrganisation(leaderId, organisationId);

        var planned = await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            organisationId,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                organisationId,
                studentId,
                Guid.CreateVersion7(),
                "Planned intervention.",
                null,
                null,
                null,
                null));

        await PatchStatusAsync(planned.Id, teacherId, organisationId, InterventionStatuses.Active);

        var activeOnly = await SendAsAsync<OrganisationInterventionsResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{organisationId}/interventions?status={InterventionStatuses.Active}",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);

        var active = Assert.Single(activeOnly.Interventions);
        Assert.Equal(planned.Id, active.Id);
        Assert.Equal(InterventionStatuses.Active, active.Status);
    }

    [Fact]
    public async Task SchoolLeader_CannotListOrganisationInterventionsForUnassignedOrganisation()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{organisationId}/interventions",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotListOrganisationInterventions()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{organisationId}/interventions",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherTeacher_CannotModifyIntervention()
    {
        var assignedTeacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(assignedTeacherId, studentId);
        _accessChecker.AllowTeacher(otherTeacherId, studentId);

        var created = await CreateSampleInterventionAsync(assignedTeacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/interventions/{created.Id}",
            otherTeacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(
            new PatchInterventionRequest(null, "Should not apply.", null, null, null, null, null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewOwnInterventions()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        await CreateSampleInterventionAsync(teacherId, studentId);

        var response = await SendAsAsync<StudentInterventionsResponse>(
            HttpMethod.Get,
            $"/api/v1/interventions?studentUserId={studentId}",
            studentId,
            TestJwt.StudentRole);

        Assert.Single(response.Interventions);
    }

    [Fact]
    public async Task Student_CannotModifyIntervention()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var created = await CreateSampleInterventionAsync(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Patch,
            $"/api/v1/interventions/{created.Id}",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(
            new PatchInterventionRequest(null, "Student notes.", null, null, null, null, null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CreatesInterventionFromLearningGap_AppearsInStudentList()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = DefaultTenant.Id;
        var learningGapId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, studentId);

        var plannedActions =
            "Address high severity gap in micro-skill abc123: guided practice and formative check.";
        var created = await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                organisationId,
                studentId,
                learningGapId,
                plannedActions,
                "Created from EI dashboard gap review.",
                null,
                null,
                null));

        Assert.Equal(learningGapId, created.LearningGapId);
        Assert.Equal(InterventionStatuses.Planned, created.Status);
        Assert.Equal(plannedActions, created.PlannedActions);

        var listResponse = await SendAsAsync<StudentInterventionsResponse>(
            HttpMethod.Get,
            $"/api/v1/interventions?studentUserId={studentId}",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Single(listResponse.Interventions);
        Assert.Equal(created.Id, listResponse.Interventions[0].Id);
        Assert.Equal(learningGapId, listResponse.Interventions[0].LearningGapId);

        var fetched = await SendAsAsync<InterventionResponse>(
            HttpMethod.Get,
            $"/api/v1/interventions/{created.Id}",
            studentId,
            TestJwt.StudentRole);

        Assert.Equal(learningGapId, fetched.LearningGapId);
        Assert.Equal(plannedActions, fetched.PlannedActions);
    }

    [Fact]
    public async Task Teacher_CannotViewInterventionsForUnassignedStudent()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/interventions?studentUserId={studentId}",
            teacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanViewApprovedInterventionSummaryForLinkedChild()
    {
        var teacherId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = DefaultTenant.Id;
        _accessChecker.AllowTeacher(teacherId, studentId);
        _accessChecker.AllowParent(parentId, studentId);

        var created = await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            organisationId,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                organisationId,
                studentId,
                Guid.CreateVersion7(),
                "Guided practice sessions.",
                "Confidential teacher notes.",
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(14),
                null));

        var summaries = await SendAsAsync<ParentInterventionsResponse>(
            HttpMethod.Get,
            $"/api/v1/interventions/parent-summary?studentUserId={studentId}",
            parentId,
            organisationId,
            TestJwt.ParentRole);

        var summary = Assert.Single(summaries.Interventions);
        Assert.Equal(created.Id, summary.Id);
        Assert.Equal("Guided practice sessions.", summary.Summary);
        Assert.Equal(InterventionStatuses.Planned, summary.Status);
    }

    [Fact]
    public async Task Parent_CannotViewInterventionNotes()
    {
        var teacherId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = DefaultTenant.Id;
        _accessChecker.AllowTeacher(teacherId, studentId);
        _accessChecker.AllowParent(parentId, studentId);

        await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            organisationId,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                organisationId,
                studentId,
                Guid.CreateVersion7(),
                "Guided practice sessions.",
                "Confidential teacher notes.",
                null,
                null,
                null));

        var summaries = await SendAsAsync<ParentInterventionsResponse>(
            HttpMethod.Get,
            $"/api/v1/interventions/parent-summary?studentUserId={studentId}",
            parentId,
            organisationId,
            TestJwt.ParentRole);

        var summary = Assert.Single(summaries.Interventions);
        Assert.DoesNotContain("Confidential", summary.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("notes", summary.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Parent_CannotViewInterventionSummaryForUnlinkedChild()
    {
        var parentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/interventions/parent-summary?studentUserId={studentId}",
            parentId,
            TestJwt.ParentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<InterventionResponse> CreateSampleInterventionAsync(string teacherId, string studentId)
    {
        return await SendAsAsync<InterventionResponse>(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            DefaultTenant.Id,
            TestJwt.TeacherRole,
            new CreateInterventionRequest(
                DefaultTenant.Id,
                studentId,
                Guid.CreateVersion7(),
                "Guided practice for target gap.",
                "Planning notes.",
                null,
                null,
                null));
    }

    private async Task<InterventionResponse> PatchStatusAsync(
        Guid interventionId,
        string teacherId,
        Guid organisationId,
        string status) =>
        await SendAsAsync<InterventionResponse>(
            HttpMethod.Patch,
            $"/api/v1/interventions/{interventionId}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole,
            new PatchInterventionRequest(status, null, null, null, null, null, null));

    private async Task<TResponse> SendAsAsync<TResponse>(
        HttpMethod method,
        string url,
        string userId,
        string role,
        object? body = null) =>
        await SendAsAsync<TResponse>(method, url, userId, DefaultTenant.Id, role, body);

    private async Task<TResponse> SendAsAsync<TResponse>(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        string role,
        object? body = null)
    {
        using var request = TestJwt.Authorized(method, url, userId, tenantId, role);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}

public class InterventionStatusTransitionTests
{
    [Theory]
    [InlineData(InterventionStatuses.Planned, InterventionStatuses.Active, true)]
    [InlineData(InterventionStatuses.Planned, InterventionStatuses.Completed, true)]
    [InlineData(InterventionStatuses.Active, InterventionStatuses.Completed, true)]
    [InlineData(InterventionStatuses.Completed, InterventionStatuses.Closed, true)]
    [InlineData(InterventionStatuses.Planned, InterventionStatuses.Closed, false)]
    [InlineData(InterventionStatuses.Closed, InterventionStatuses.Active, false)]
    public void CanTransition_RespectsLifecycle(string current, string next, bool expected) =>
        Assert.Equal(expected, InterventionStatusTransitions.CanTransition(current, next));
}
