using System.Net;
using System.Net.Http.Json;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public class OrganisationEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeStudentLearningProfileClient _profileClient;
    private readonly FakeAssessmentDashboardClient _assessmentClient;
    private readonly FakeEvidenceDashboardClient _evidenceClient;

    public OrganisationEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _profileClient = factory.ProfileClient;
        _assessmentClient = factory.AssessmentClient;
        _evidenceClient = factory.EvidenceClient;
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/organisations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateOrganisation()
    {
        var teacherId = Guid.NewGuid().ToString();
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/organisations",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest("Denied School", UniqueCode("DEN")));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCrudOrganisationYearLevelAndClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var code = UniqueCode("SCH");

        var createdOrg = await CreateOrganisationAsAdminAsync(adminId, "Riverside School", code);
        Assert.Equal("Riverside School", createdOrg.Name);
        Assert.Equal(code, createdOrg.Code);

        var listed = await SendAsAsync<List<OrganisationResponse>>(
            HttpMethod.Get,
            "/api/v1/organisations",
            adminId,
            null,
            TestJwt.AdminRole);
        Assert.Contains(listed, org => org.Id == createdOrg.Id);

        var fetched = await SendAsAsync<OrganisationResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        Assert.Equal(createdOrg.Id, fetched.Id);

        using var updateRequest = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        updateRequest.Content = JsonContent.Create(new UpdateOrganisationRequest("Riverside Primary", code));
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedOrg = await updateResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.Equal("Riverside Primary", updatedOrg!.Name);

        var yearLevel = await CreateYearLevelAsAdminAsync(adminId, createdOrg.Id, "Year 7", 7);
        Assert.Equal("Year 7", yearLevel.Name);
        Assert.Equal(createdOrg.Id, yearLevel.OrganisationId);

        using var updateYearRequest = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/organisations/{createdOrg.Id}/year-levels/{yearLevel.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        updateYearRequest.Content = JsonContent.Create(new UpdateYearLevelRequest("Year 8", 8));
        var updateYearResponse = await _client.SendAsync(updateYearRequest);
        Assert.Equal(HttpStatusCode.OK, updateYearResponse.StatusCode);

        var schoolClass = await CreateClassAsAdminAsync(adminId, createdOrg.Id, yearLevel.Id, "8A", UniqueCode("8A"));
        Assert.Equal("8A", schoolClass.Name);
        Assert.Equal(yearLevel.Id, schoolClass.YearLevelId);

        using var updateClassRequest = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/organisations/{createdOrg.Id}/classes/{schoolClass.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        updateClassRequest.Content = JsonContent.Create(
            new UpdateClassRequest("8B", schoolClass.Code, yearLevel.Id));
        var updateClassResponse = await _client.SendAsync(updateClassRequest);
        Assert.Equal(HttpStatusCode.OK, updateClassResponse.StatusCode);

        using var deleteClass = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/organisations/{createdOrg.Id}/classes/{schoolClass.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteClass)).StatusCode);

        using var deleteYear = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/organisations/{createdOrg.Id}/year-levels/{yearLevel.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteYear)).StatusCode);

        using var deleteOrg = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteOrg)).StatusCode);

        using var missing = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            createdOrg.Id,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);
    }

    [Fact]
    public async Task Admin_CanAssignTeacherAndEnrollStudent()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Harbour School", UniqueCode("HAR"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 5", 5);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "5A", UniqueCode("5A"));

        using var assign = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/teachers",
            adminId,
            org.Id,
            TestJwt.AdminRole);
        assign.Content = JsonContent.Create(new AssignTeacherRequest(teacherId));
        var assignResponse = await _client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        using var enroll = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/enrollments",
            adminId,
            org.Id,
            TestJwt.AdminRole);
        enroll.Content = JsonContent.Create(new EnrollStudentRequest(studentId));
        var enrollResponse = await _client.SendAsync(enroll);
        Assert.Equal(HttpStatusCode.Created, enrollResponse.StatusCode);

        var teachers = await SendAsAsync<List<ClassMemberResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/teachers",
            adminId,
            org.Id,
            TestJwt.AdminRole);
        Assert.Contains(teachers, member => member.UserId == teacherId);

        var enrollments = await SendAsAsync<List<ClassMemberResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/enrollments",
            adminId,
            org.Id,
            TestJwt.AdminRole);
        Assert.Contains(enrollments, member => member.UserId == studentId);
        Assert.Contains(_profileClient.SyncCalls, call =>
            call.StudentUserId == studentId && call.Enrollment.ClassId == schoolClass.Id);
    }

    [Fact]
    public async Task EnrollStudent_AutoCreatesStudentLearningProfile()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Bayview School", UniqueCode("BAY"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 1", 1);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "1A", UniqueCode("1A"));

        _profileClient.SyncCalls.Clear();
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);

        var sync = Assert.Single(_profileClient.SyncCalls);
        Assert.Equal(studentId, sync.StudentUserId);
        Assert.Equal(org.Id, sync.Enrollment.OrganisationId);
        Assert.Equal(schoolClass.Id, sync.Enrollment.ClassId);
        Assert.Equal("1A", sync.Enrollment.ClassName);
        Assert.Equal(schoolClass.Code, sync.Enrollment.ClassCode);
    }

    [Fact]
    public async Task Teacher_SeesOnlyAssignedClasses()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Oakridge School", UniqueCode("OAK"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 3", 3);
        var assigned = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "3A", UniqueCode("3A"));
        var other = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "3B", UniqueCode("3B"));

        await AssignTeacherAsync(adminId, org.Id, assigned.Id, teacherId);
        await AssignTeacherAsync(adminId, org.Id, other.Id, otherTeacherId);

        var classes = await SendAsAsync<List<ClassResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes",
            teacherId,
            org.Id,
            TestJwt.TeacherRole);

        Assert.Contains(classes, item => item.Id == assigned.Id);
        Assert.DoesNotContain(classes, item => item.Id == other.Id);
    }

    [Fact]
    public async Task Student_SeesOnlyOwnEnrollment()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Maple School", UniqueCode("MAP"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 4", 4);
        var enrolledClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "4A", UniqueCode("4A"));
        var otherClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "4B", UniqueCode("4B"));

        await EnrollStudentAsync(adminId, org.Id, enrolledClass.Id, studentId);
        await EnrollStudentAsync(adminId, org.Id, otherClass.Id, otherStudentId);

        var classes = await SendAsAsync<List<ClassResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes",
            studentId,
            org.Id,
            TestJwt.StudentRole);

        Assert.Contains(classes, item => item.Id == enrolledClass.Id);
        Assert.DoesNotContain(classes, item => item.Id == otherClass.Id);

        var enrollments = await SendAsAsync<List<ClassMemberResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{enrolledClass.Id}/enrollments",
            studentId,
            org.Id,
            TestJwt.StudentRole);

        Assert.Single(enrollments);
        Assert.Equal(studentId, enrollments[0].UserId);
    }

    [Fact]
    public async Task Student_GetClass_ListsOnlyThemselvesAsStudent()
    {
        // Assessment and evidence services admit a student only when their id is in StudentUserIds.
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var classmateId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Birch School", UniqueCode("BIR"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 11", 11);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "11MAT", UniqueCode("11M"));

        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, classmateId);

        var fetched = await SendAsAsync<ClassResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}",
            studentId,
            org.Id,
            TestJwt.StudentRole);

        Assert.Equal([studentId], fetched.StudentUserIds);
    }

    [Fact]
    public async Task Teacher_IsDeniedCrossClassAccess()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Cedar School", UniqueCode("CED"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 6", 6);
        var assigned = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "6A", UniqueCode("6A"));
        var other = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "6B", UniqueCode("6B"));

        await AssignTeacherAsync(adminId, org.Id, assigned.Id, teacherId);
        await AssignTeacherAsync(adminId, org.Id, other.Id, otherTeacherId);

        using var classRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}",
            teacherId,
            org.Id,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(classRequest)).StatusCode);

        using var enrollmentsRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}/enrollments",
            teacherId,
            org.Id,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(enrollmentsRequest)).StatusCode);
    }

    [Fact]
    public async Task Teacher_CanViewClassDashboardForAssignedClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var assessmentId = Guid.CreateVersion7();
        var latestActivity = DateTimeOffset.UtcNow;

        var org = await CreateOrganisationAsAdminAsync(adminId, "Willow School", UniqueCode("WIL"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 5", 5);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "5A", UniqueCode("5A"));

        await AssignTeacherAsync(adminId, org.Id, schoolClass.Id, teacherId);
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);

        _assessmentClient.Summaries =
        [
            new AssessmentSummaryData(assessmentId, "Unit Quiz", "Published", DateTimeOffset.UtcNow.AddDays(3), 2)
        ];
        _evidenceClient.Summaries = [new EvidenceSummaryData(assessmentId, 1)];
        _profileClient.Summaries[studentId] = new StudentProfileSummaryData(studentId, 3, latestActivity);

        var dashboard = await SendAsAsync<ClassDashboardResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/dashboard",
            teacherId,
            org.Id,
            TestJwt.TeacherRole);

        Assert.Equal(schoolClass.Id, dashboard.Class.Id);
        Assert.Single(dashboard.Roster);
        Assert.Equal(studentId, dashboard.Roster[0].StudentUserId);
        Assert.Equal(3, dashboard.Roster[0].EvidenceCount);
        Assert.Equal(latestActivity, dashboard.Roster[0].LatestActivityAt);
        Assert.Single(dashboard.RecentAssessments);
        Assert.Equal("Unit Quiz", dashboard.RecentAssessments[0].Title);
        Assert.Equal(2, dashboard.RecentAssessments[0].SubmissionCount);
        Assert.Equal(1, dashboard.RecentAssessments[0].ReviewedCount);
    }

    [Fact]
    public async Task Teacher_CannotViewClassDashboardForUnassignedClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Birch School", UniqueCode("BIR"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var assigned = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "7A", UniqueCode("7A"));
        var other = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "7B", UniqueCode("7B"));

        await AssignTeacherAsync(adminId, org.Id, assigned.Id, teacherId);
        await AssignTeacherAsync(adminId, org.Id, other.Id, otherTeacherId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}/dashboard",
            teacherId,
            org.Id,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Student_CannotViewClassDashboard()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Elm School", UniqueCode("ELM"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 8", 8);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "8A", UniqueCode("8A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/dashboard",
            studentId,
            org.Id,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Student_CanViewOwnWorkspace()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var assessmentId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        var learningObjectiveId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var timelineId = Guid.CreateVersion7();
        var recordedAt = DateTimeOffset.UtcNow;

        var org = await CreateOrganisationAsAdminAsync(adminId, "Oak School", UniqueCode("OAK"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 6", 6);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "6A", UniqueCode("6A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);

        _assessmentClient.StudentSummaries =
        [
            new StudentAssessmentSummaryData(
                assessmentId,
                "Fractions quiz",
                DateTimeOffset.UtcNow.AddDays(2),
                [learningObjectiveId],
                false,
                null)
        ];
        _evidenceClient.StudentFeedback =
        [
            new StudentEvidenceFeedbackData(
                evidenceId,
                assessmentId,
                "Fractions quiz",
                recordedAt,
                [new StudentEvidenceFeedbackMarkData(microSkillId, 4, "Great work.")])
        ];
        _profileClient.Profiles[studentId] = new StudentProfileData(
            studentId,
            [new StudentProfileTimelineEntryData(timelineId, "Fractions quiz", recordedAt)]);

        var workspace = await SendAsAsync<StudentWorkspaceResponse>(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/workspace",
            studentId,
            null,
            TestJwt.StudentRole);

        Assert.Equal(studentId, workspace.StudentUserId);
        Assert.Single(workspace.Assessments);
        Assert.Equal("Fractions quiz", workspace.Assessments[0].Title);
        Assert.Equal(schoolClass.Id, workspace.Assessments[0].ClassId);
        Assert.Contains(learningObjectiveId, workspace.Assessments[0].LearningObjectiveIds);
        Assert.False(workspace.Assessments[0].HasSubmitted);
        Assert.Single(workspace.Feedback);
        Assert.Equal("Great work.", workspace.Feedback[0].MicroSkillMarks[0].Feedback);
        Assert.Single(workspace.Timeline);
        Assert.Equal(timelineId, workspace.Timeline[0].Id);
        Assert.Contains((org.Id, schoolClass.Id), _assessmentClient.RequestedStudentClasses);
    }

    [Fact]
    public async Task Student_CannotViewOtherStudentWorkspace()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Maple School", UniqueCode("MAP"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 4", 4);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "4A", UniqueCode("4A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, otherStudentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{otherStudentId}/workspace",
            studentId,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotViewStudentWorkspace()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Cedar School", UniqueCode("CED"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 3", 3);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "3A", UniqueCode("3A"));
        await AssignTeacherAsync(adminId, org.Id, schoolClass.Id, teacherId);
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/workspace",
            teacherId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task Student_IsDeniedCrossClassAccess()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Pine School", UniqueCode("PIN"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 2", 2);
        var enrolled = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "2A", UniqueCode("2A"));
        var other = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "2B", UniqueCode("2B"));

        await EnrollStudentAsync(adminId, org.Id, enrolled.Id, studentId);
        await EnrollStudentAsync(adminId, org.Id, other.Id, otherStudentId);

        using var classRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}",
            studentId,
            org.Id,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(classRequest)).StatusCode);

        using var enrollmentsRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}/enrollments",
            studentId,
            org.Id,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(enrollmentsRequest)).StatusCode);
    }

    private async Task<OrganisationResponse> CreateOrganisationAsAdminAsync(string adminId, string name, string code)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/organisations", adminId, TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest(name, code));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OrganisationResponse>();
        return payload ?? throw new InvalidOperationException("Missing organisation payload.");
    }

    private async Task<YearLevelResponse> CreateYearLevelAsAdminAsync(
        string adminId,
        Guid organisationId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/year-levels",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateYearLevelRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<YearLevelResponse>();
        return payload ?? throw new InvalidOperationException("Missing year level payload.");
    }

    private async Task<ClassResponse> CreateClassAsAdminAsync(
        string adminId,
        Guid organisationId,
        Guid yearLevelId,
        string name,
        string code)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateClassRequest(name, code, yearLevelId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ClassResponse>();
        return payload ?? throw new InvalidOperationException("Missing class payload.");
    }

    private async Task AssignTeacherAsync(string adminId, Guid organisationId, Guid classId, string teacherId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/teachers",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new AssignTeacherRequest(teacherId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task EnrollStudentAsync(string adminId, Guid organisationId, Guid classId, string studentId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/enrollments",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new EnrollStudentRequest(studentId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, Guid? tenantId, string role)
    {
        using var request = tenantId.HasValue
            ? TestJwt.Authorized(method, url, userId, tenantId.Value, role)
            : TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<T>();
        return payload ?? throw new InvalidOperationException("Missing response payload.");
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
