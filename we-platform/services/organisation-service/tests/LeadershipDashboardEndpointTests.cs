using System.Net;
using System.Net.Http.Json;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public class LeadershipDashboardEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeAssessmentDashboardClient _assessmentClient;
    private readonly FakeInterventionDashboardClient _interventionClient;
    private readonly FakeEiInsightsClient _eiClient;

    public LeadershipDashboardEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _assessmentClient = factory.AssessmentClient;
        _interventionClient = factory.InterventionClient;
        _eiClient = factory.EiClient;
    }

    [Fact]
    public async Task SchoolLeader_CanViewLeadershipDashboardForAssignedOrganisation()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var microSkillId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Summit School", UniqueCode("SUM"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "7A", UniqueCode("7A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _assessmentClient.Summaries =
        [
            new AssessmentSummaryData(assessmentId, "Term Quiz", "Published", DateTimeOffset.UtcNow.AddDays(5), 1)
        ];
        _interventionClient.InterventionsByStudent[studentId] =
        [
            new ParentInterventionSummaryData(
                Guid.CreateVersion7(),
                "Guided reading support",
                "Active",
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow.AddDays(14))
        ];
        _eiClient.InsightsByClass[(org.Id, schoolClass.Id)] = new ClassEiInsightsData(
            org.Id,
            schoolClass.Id,
            [
                new ClassMasteryDistributionData(
                    microSkillId,
                    new Dictionary<string, int> { ["Developing"] = 1 },
                    1,
                    "One student developing.",
                    [])
            ],
            [
                new ClassActiveGapData(
                    Guid.CreateVersion7(),
                    microSkillId,
                    "High",
                    "Medium",
                    studentId,
                    "Gap detected from recent evidence.",
                    Guid.CreateVersion7())
            ],
            [],
            [
                new StudentNeedingAttentionData(studentId, "Active learning gap", "High severity gap.", null)
            ]);

        var dashboard = await SendAsAsync<LeadershipDashboardResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            leaderId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(org.Id, dashboard.OrganisationId);
        Assert.Equal("Summit School", dashboard.OrganisationName);
        Assert.Equal(1, dashboard.Kpis.TotalStudents);
        Assert.Equal(1, dashboard.Kpis.TotalClasses);
        Assert.Equal(1, dashboard.Kpis.ActiveInterventions);
        Assert.Equal(1, dashboard.Kpis.ActiveLearningGaps);
        Assert.Equal(1, dashboard.Kpis.StudentsNeedingAttention);
        Assert.Contains(dashboard.Kpis.MasteryLevelCounts, pair => pair.Key == "Developing" && pair.Value == 1);
        Assert.Single(dashboard.YearLevels);
        Assert.Equal("Year 7", dashboard.YearLevels[0].YearLevelName);
        Assert.Single(dashboard.ClassComparisons);
        Assert.Equal("7A", dashboard.ClassComparisons[0].ClassName);
        Assert.Contains(_assessmentClient.RequestedClasses, pair => pair.ClassId == schoolClass.Id);
        Assert.Contains(_eiClient.RequestedClasses, pair => pair.ClassId == schoolClass.Id);
    }

    [Fact]
    public async Task Teacher_CannotViewLeadershipDashboard()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Valley School", UniqueCode("VAL"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 5", 5);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "5A", UniqueCode("5A"));
        await AssignTeacherAsync(adminId, org.Id, schoolClass.Id, teacherId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            teacherId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CannotViewLeadershipDashboardForUnassignedOrganisation()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Ridge School", UniqueCode("RID"));
        await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 6", 6);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/dashboard",
            leaderId,
            TestJwt.SchoolLeaderRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CanDrillDownToYearLevel()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Harbour School", UniqueCode("HAR"));
        var year7 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var year8 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 8", 8);
        var class7A = await CreateClassAsAdminAsync(adminId, org.Id, year7.Id, "7A", UniqueCode("7A"));
        var class8A = await CreateClassAsAdminAsync(adminId, org.Id, year8.Id, "8A", UniqueCode("8A"));
        await EnrollStudentAsync(adminId, org.Id, class7A.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _eiClient.InsightsByClass[(org.Id, class7A.Id)] = EmptyInsights(org.Id, class7A.Id);
        _eiClient.InsightsByClass[(org.Id, class8A.Id)] = EmptyInsights(org.Id, class8A.Id);

        var dashboard = await SendAsAsync<YearLevelLeadershipDashboardResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/year-levels/{year7.Id}/leadership/dashboard",
            leaderId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(year7.Id, dashboard.YearLevelId);
        Assert.Equal("Year 7", dashboard.YearLevelName);
        Assert.Single(dashboard.ClassComparisons);
        Assert.Equal(class7A.Id, dashboard.ClassComparisons[0].ClassId);
    }

    [Fact]
    public async Task SchoolLeader_CanDrillDownToClassSummary()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var assessmentId = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Cove School", UniqueCode("COV"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 9", 9);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "9A", UniqueCode("9A"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _assessmentClient.Summaries =
        [
            new AssessmentSummaryData(assessmentId, "Algebra check", "Published", DateTimeOffset.UtcNow.AddDays(1), 1)
        ];
        _eiClient.InsightsByClass[(org.Id, schoolClass.Id)] = EmptyInsights(org.Id, schoolClass.Id);

        var summary = await SendAsAsync<ClassLeadershipSummaryResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/leadership/summary",
            leaderId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(schoolClass.Id, summary.Class.Id);
        Assert.Single(summary.Students);
        Assert.Equal(studentId, summary.Students[0].UserId);
        Assert.Single(summary.RecentAssessments);
        Assert.Equal("Algebra check", summary.RecentAssessments[0].Title);
    }

    [Fact]
    public async Task SchoolLeader_CanListOrganisationsAndClassesInAssignedSchool()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Lake School", UniqueCode("LAK"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 4", 4);
        await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "4A", UniqueCode("4A"));
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        var organisations = await SendAsAsync<List<OrganisationResponse>>(
            HttpMethod.Get,
            "/api/v1/organisations",
            leaderId,
            TestJwt.SchoolLeaderRole);
        Assert.Single(organisations);
        Assert.Equal(org.Id, organisations[0].Id);

        var classes = await SendAsAsync<List<ClassResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes",
            leaderId,
            TestJwt.SchoolLeaderRole);
        Assert.Single(classes);
    }

    [Fact]
    public async Task SchoolLeader_CanListLeadershipInterventionsWithAggregation()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var learningGapId = Guid.CreateVersion7();
        var interventionId = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Pine School", UniqueCode("PIN"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "7B", UniqueCode("7B"));
        await EnrollStudentAsync(adminId, org.Id, schoolClass.Id, studentId);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _interventionClient.OrganisationInterventions[(org.Id, null)] =
        [
            new InterventionDetailData(
                interventionId,
                org.Id,
                studentId,
                learningGapId,
                teacherId,
                "Guided reading support.",
                null,
                "Active",
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow.AddDays(14),
                null,
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow)
        ];
        _eiClient.InsightsByClass[(org.Id, schoolClass.Id)] = new ClassEiInsightsData(
            org.Id,
            schoolClass.Id,
            [],
            [
                new ClassActiveGapData(
                    learningGapId,
                    Guid.CreateVersion7(),
                    "High",
                    "Medium",
                    studentId,
                    "Gap detected.",
                    Guid.CreateVersion7())
            ],
            [],
            []);

        var monitoring = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions",
            leaderId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(org.Id, monitoring.OrganisationId);
        var item = Assert.Single(monitoring.Interventions);
        Assert.Equal(interventionId, item.InterventionId);
        Assert.Equal(studentId, item.StudentUserId);
        Assert.Equal(learningGapId, item.LearningGapId);
        Assert.Equal(teacherId, item.AssignedTeacherUserId);
        Assert.Equal("Active", item.Status);
        Assert.Equal(schoolClass.Id, item.ClassId);
        Assert.Equal("7B", item.ClassName);
        Assert.Equal(year.Id, item.YearLevelId);
        Assert.Equal("Year 7", item.YearLevelName);
        Assert.Equal("High", item.GapSeverity);
    }

    [Fact]
    public async Task SchoolLeader_CanFilterLeadershipInterventionsByClassAndSeverity()
    {
        var adminId = Guid.NewGuid().ToString();
        var leaderId = Guid.NewGuid().ToString();
        var student7 = Guid.NewGuid().ToString();
        var student8 = Guid.NewGuid().ToString();
        var gap7 = Guid.CreateVersion7();
        var gap8 = Guid.CreateVersion7();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Oak School", UniqueCode("OAK"));
        var year7 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 7", 7);
        var year8 = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 8", 8);
        var class7A = await CreateClassAsAdminAsync(adminId, org.Id, year7.Id, "7A", UniqueCode("7A"));
        var class8A = await CreateClassAsAdminAsync(adminId, org.Id, year8.Id, "8A", UniqueCode("8A"));
        await EnrollStudentAsync(adminId, org.Id, class7A.Id, student7);
        await EnrollStudentAsync(adminId, org.Id, class8A.Id, student8);
        await AssignSchoolLeaderAsync(adminId, org.Id, leaderId);

        _interventionClient.OrganisationInterventions[(org.Id, null)] =
        [
            new InterventionDetailData(
                Guid.CreateVersion7(), org.Id, student7, gap7, Guid.NewGuid().ToString(),
                "Year 7 support.", null, "Active", null, null, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
            new InterventionDetailData(
                Guid.CreateVersion7(), org.Id, student8, gap8, Guid.NewGuid().ToString(),
                "Year 8 support.", null, "Planned", null, null, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        ];
        _eiClient.InsightsByClass[(org.Id, class7A.Id)] = new ClassEiInsightsData(
            org.Id, class7A.Id, [], [new ClassActiveGapData(gap7, Guid.CreateVersion7(), "High", "Medium", student7, "", Guid.CreateVersion7())], [], []);
        _eiClient.InsightsByClass[(org.Id, class8A.Id)] = new ClassEiInsightsData(
            org.Id, class8A.Id, [], [new ClassActiveGapData(gap8, Guid.CreateVersion7(), "Low", "Low", student8, "", Guid.CreateVersion7())], [], []);

        var classFiltered = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions?classId={class7A.Id}",
            leaderId,
            TestJwt.SchoolLeaderRole);
        Assert.Single(classFiltered.Interventions);
        Assert.Equal(student7, classFiltered.Interventions[0].StudentUserId);

        var severityFiltered = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions?severity=High",
            leaderId,
            TestJwt.SchoolLeaderRole);
        Assert.Single(severityFiltered.Interventions);
        Assert.Equal("High", severityFiltered.Interventions[0].GapSeverity);

        var yearFiltered = await SendAsAsync<LeadershipInterventionMonitoringResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions?yearLevelId={year8.Id}",
            leaderId,
            TestJwt.SchoolLeaderRole);
        Assert.Single(yearFiltered.Interventions);
        Assert.Equal(student8, yearFiltered.Interventions[0].StudentUserId);
    }

    [Fact]
    public async Task Teacher_CannotListLeadershipInterventions()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Elm School", UniqueCode("ELM"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 6", 6);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "6A", UniqueCode("6A"));
        await AssignTeacherAsync(adminId, org.Id, schoolClass.Id, teacherId);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/leadership/interventions",
            teacherId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);
    }

    private static ClassEiInsightsData EmptyInsights(Guid organisationId, Guid classId) =>
        new(organisationId, classId, [], [], [], []);

    private async Task<OrganisationResponse> CreateOrganisationAsAdminAsync(string adminId, string name, string code)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/organisations", adminId, TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest(name, code));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganisationResponse>())!;
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
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateYearLevelRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<YearLevelResponse>())!;
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
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateClassRequest(name, code, yearLevelId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassResponse>())!;
    }

    private async Task EnrollStudentAsync(string adminId, Guid organisationId, Guid classId, string studentId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/enrollments",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new EnrollStudentRequest(studentId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task AssignTeacherAsync(string adminId, Guid organisationId, Guid classId, string teacherId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/teachers",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new AssignTeacherRequest(teacherId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task AssignSchoolLeaderAsync(string adminId, Guid organisationId, string leaderId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/leaders",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new AssignSchoolLeaderRequest(leaderId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role)
    {
        using var request = TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
