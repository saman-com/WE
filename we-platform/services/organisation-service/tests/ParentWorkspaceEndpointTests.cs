using System.Net;
using System.Net.Http.Json;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public class ParentWorkspaceEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeAssessmentDashboardClient _assessmentClient;
    private readonly FakeEvidenceDashboardClient _evidenceClient;
    private readonly FakeMasteryDashboardClient _masteryClient;
    private readonly FakeInterventionDashboardClient _interventionClient;

    public ParentWorkspaceEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _assessmentClient = factory.AssessmentClient;
        _evidenceClient = factory.EvidenceClient;
        _masteryClient = factory.MasteryClient;
        _interventionClient = factory.InterventionClient;
    }

    [Fact]
    public async Task Admin_LinksParentToStudent()
    {
        var adminId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/parents/{parentId}/children",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new LinkParentStudentRequest(studentId));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var linked = await response.Content.ReadFromJsonAsync<ParentChildLinkResponse>();
        Assert.Equal(parentId, linked!.ParentUserId);
        Assert.Equal(studentId, linked.StudentUserId);

        var children = await SendAsAsync<List<ParentChildLinkResponse>>(
            HttpMethod.Get,
            $"/api/v1/parents/{parentId}/children",
            adminId,
            null,
            TestJwt.AdminRole);
        Assert.Contains(children, item => item.StudentUserId == studentId);
    }

    [Fact]
    public async Task Parent_CanViewLinkedChildProgress()
    {
        var adminId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var assessmentId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var interventionId = Guid.CreateVersion7();
        var recordedAt = DateTimeOffset.UtcNow;

        var org = await CreateOrganisationAsAdminAsync(adminId, "Parent School", UniqueCode("PAR"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 5", 5);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "5A", UniqueCode("5A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await LinkParentToStudentAsync(adminId, parentId, studentId);

        _assessmentClient.StudentSummaries =
        [
            new StudentAssessmentSummaryData(
                assessmentId,
                "Fractions quiz",
                DateTimeOffset.UtcNow.AddDays(2),
                [],
                true,
                recordedAt)
        ];
        _evidenceClient.StudentFeedbackByStudent[studentId] =
        [
            new StudentEvidenceFeedbackData(
                evidenceId,
                assessmentId,
                "Fractions quiz",
                recordedAt,
                [new StudentEvidenceFeedbackMarkData(microSkillId, 4, "Strong understanding.")])
        ];
        _masteryClient.RecordsByStudent[studentId] =
        [
            new ParentMasterySummaryData(microSkillId, "Developing")
        ];
        _interventionClient.InterventionsByStudent[studentId] =
        [
            new ParentInterventionSummaryData(
                interventionId,
                "Guided practice sessions.",
                "Active",
                recordedAt,
                null)
        ];

        var progress = await SendAsAsync<ParentChildProgressResponse>(
            HttpMethod.Get,
            $"/api/v1/parents/me/children/{studentId}/progress",
            parentId,
            null,
            TestJwt.ParentRole);

        Assert.Equal(studentId, progress.StudentUserId);
        Assert.Single(progress.Assessments);
        Assert.Equal("Fractions quiz", progress.Assessments[0].Title);
        Assert.True(progress.Assessments[0].HasSubmitted);
        Assert.Single(progress.Feedback);
        Assert.Equal("Strong understanding.", progress.Feedback[0].MicroSkillMarks[0].Feedback);
        Assert.Single(progress.Mastery);
        Assert.Equal("Developing", progress.Mastery[0].MasteryLevel);
        Assert.Single(progress.ActiveInterventions);
        Assert.Equal("Guided practice sessions.", progress.ActiveInterventions[0].Summary);
        Assert.DoesNotContain(
            progress.ActiveInterventions,
            item => item.Summary.Contains("confidential", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Parent_CannotViewUnlinkedChildProgress()
    {
        var parentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/parents/me/children/{studentId}/progress",
            parentId,
            TestJwt.ParentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotViewOtherStudentsLinkedChildProgress()
    {
        var adminId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var otherParentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        await LinkParentToStudentAsync(adminId, otherParentId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/parents/me/children/{studentId}/progress",
            parentId,
            TestJwt.ParentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanListLinkedChildren()
    {
        var adminId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var firstStudentId = Guid.NewGuid().ToString();
        var secondStudentId = Guid.NewGuid().ToString();

        await LinkParentToStudentAsync(adminId, parentId, firstStudentId);
        await LinkParentToStudentAsync(adminId, parentId, secondStudentId);

        var children = await SendAsAsync<List<ParentChildLinkResponse>>(
            HttpMethod.Get,
            "/api/v1/parents/me/children",
            parentId,
            null,
            TestJwt.ParentRole);

        Assert.Equal(2, children.Count);
        Assert.Contains(children, item => item.StudentUserId == firstStudentId);
        Assert.Contains(children, item => item.StudentUserId == secondStudentId);
    }

    private async Task<OrganisationResponse> CreateOrganisationAsAdminAsync(string adminId, string name, string code)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/organisations", adminId, TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest(name, code));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganisationResponse>())
            ?? throw new InvalidOperationException("Missing organisation payload.");
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
        return (await response.Content.ReadFromJsonAsync<YearLevelResponse>())
            ?? throw new InvalidOperationException("Missing year level payload.");
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
        return (await response.Content.ReadFromJsonAsync<ClassResponse>())
            ?? throw new InvalidOperationException("Missing class payload.");
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

    [Fact]
    public async Task Parent_CanConfirmTeacherAccessForLinkedChild()
    {
        var adminId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Message School", UniqueCode("MSG"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 11", 11);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "11A", UniqueCode("11A"));
        await AssignTeacherAsync(adminId, org.Id, schoolClass.Id, teacherId);
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await LinkParentToStudentAsync(adminId, parentId, studentId);

        using var allowed = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/access/teacher/{teacherId}/student/{studentId}",
            parentId,
            org.Id,
            TestJwt.ParentRole);
        var allowedResponse = await _client.SendAsync(allowed);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);

        using var denied = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/access/teacher/{Guid.NewGuid()}/student/{studentId}",
            parentId,
            org.Id,
            TestJwt.ParentRole);
        var deniedResponse = await _client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
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

    private async Task LinkParentToStudentAsync(string adminId, string parentId, string studentId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/parents/{parentId}/children",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new LinkParentStudentRequest(studentId));
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
        return (await response.Content.ReadFromJsonAsync<T>())
            ?? throw new InvalidOperationException("Missing response payload.");
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
