using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ReportingService.Application;
using ReportingService.Domain;

namespace ReportingService.Tests;

public class ReportingEndpointTests : IClassFixture<ReportingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly FakeEiInsightsClient _eiClient;
    private readonly FakeClassDashboardClient _dashboardClient;
    private readonly FakeSchoolSummaryClient _schoolSummaryClient;

    public ReportingEndpointTests(ReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _eiClient = factory.EiInsightsClient;
        _dashboardClient = factory.ClassDashboardClient;
        _schoolSummaryClient = factory.SchoolSummaryClient;
    }

    [Fact]
    public async Task Teacher_CanGenerateClassProgressReport_WithSeedData()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        var assessmentId = Guid.CreateVersion7();
        var generatedAt = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _eiClient.SetResponse(
            organisationId,
            classId,
            new ClassEiInsightsData(
                organisationId,
                classId,
                [
                    new ClassMasteryDistributionSection(
                        microSkillId,
                        new Dictionary<string, int>
                        {
                            ["Mastered"] = 2,
                            ["Developing"] = 1
                        },
                        3,
                        "Class mastery distribution from EI.",
                        [evidenceId])
                ],
                [
                    new ClassActiveGapSection(
                        Guid.CreateVersion7(),
                        microSkillId,
                        "High",
                        "High",
                        "student-1",
                        "Gap explanation from EI.",
                        evidenceId)
                ]));
        _dashboardClient.SetResponse(
            organisationId,
            classId,
            new ClassDashboardData(
                classId,
                "Year 7 Science",
                [
                    new AssessmentSummarySection(
                        assessmentId,
                        "Forces quiz",
                        "Published",
                        3,
                        2,
                        generatedAt.AddDays(-7))
                ]));

        var report = await SendAsAsync<ReportResponse>(
            HttpMethod.Post,
            $"/api/v1/reports/organisations/{organisationId}/classes/{classId}/class-progress",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);

        Assert.Equal(ReportTypes.ClassProgress, report.ReportType);
        Assert.Equal(classId, report.ClassId);
        Assert.Equal(teacherId, report.RequestedByUserId);

        var content = DeserializeContent<ClassProgressReportContent>(report.Content);
        Assert.Equal("Year 7 Science", content.ClassName);
        Assert.Single(content.MasteryDistribution);
        Assert.Single(content.ActiveLearningGaps);
        Assert.Equal("student-1", content.ActiveLearningGaps[0].StudentUserId);
        Assert.Single(content.AssessmentSummary);
        Assert.Equal("Forces quiz", content.AssessmentSummary[0].Title);
    }

    [Fact]
    public async Task Teacher_CannotGenerateClassProgressReport_ForUnassignedClass()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/reports/organisations/{organisationId}/classes/{classId}/class-progress",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CanGenerateSchoolSummaryReport()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var yearLevelId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();

        _accessChecker.AllowSchoolLeader(leaderId, organisationId);
        _schoolSummaryClient.SetResponse(
            organisationId,
            new SchoolSummaryData(
                organisationId,
                "Northside Secondary",
                new LeadershipKpiSection(
                    120,
                    6,
                    8,
                    15,
                    4,
                    0.82m,
                    new Dictionary<string, int> { ["Mastered"] = 40, ["Developing"] = 30 }),
                [
                    new YearLevelSummarySection(
                        yearLevelId,
                        "Year 7",
                        2,
                        40,
                        3,
                        5)
                ],
                [
                    new ClassComparisonSection(
                        classId,
                        "7A Science",
                        yearLevelId,
                        "Year 7",
                        20,
                        2,
                        3,
                        1,
                        0.85m)
                ]));

        var report = await SendAsAsync<ReportResponse>(
            HttpMethod.Post,
            $"/api/v1/reports/organisations/{organisationId}/school-summary",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(ReportTypes.SchoolSummary, report.ReportType);
        Assert.Null(report.ClassId);
        Assert.Equal(leaderId, report.RequestedByUserId);

        var content = DeserializeContent<SchoolSummaryReportContent>(report.Content);
        Assert.Equal("Northside Secondary", content.OrganisationName);
        Assert.Equal(120, content.Kpis.TotalStudents);
        Assert.Single(content.YearLevels);
        Assert.Equal("Year 7", content.YearLevels[0].YearLevelName);
        Assert.Single(content.ClassComparisons);
        Assert.Equal("7A Science", content.ClassComparisons[0].ClassName);
    }

    [Fact]
    public async Task SchoolLeader_CannotGenerateSchoolSummaryReport_ForUnassignedOrganisation()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/reports/organisations/{organisationId}/school-summary",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GeneratedReport_CanExportAsPdf()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _eiClient.SetResponse(
            organisationId,
            classId,
            new ClassEiInsightsData(
                organisationId,
                classId,
                [
                    new ClassMasteryDistributionSection(
                        microSkillId,
                        new Dictionary<string, int> { ["Mastered"] = 1 },
                        1,
                        "Mastery section.",
                        [])
                ],
                []));
        _dashboardClient.SetResponse(
            organisationId,
            classId,
            new ClassDashboardData(classId, "7B Maths", []));

        var report = await SendAsAsync<ReportResponse>(
            HttpMethod.Post,
            $"/api/v1/reports/organisations/{organisationId}/classes/{classId}/class-progress",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);

        using var pdfRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/reports/{report.Id}/pdf",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var pdfResponse = await _client.SendAsync(pdfRequest);

        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);

        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes[..4]));
    }

    [Fact]
    public async Task Student_CannotGenerateClassProgressReport()
    {
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/reports/organisations/{organisationId}/classes/{classId}/class-progress",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        params string[] roles)
    {
        using var request = TestJwt.Authorized(method, url, userId, tenantId, roles);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(payload);
        return payload;
    }

    private static T DeserializeContent<T>(object content)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(content, options);
        var deserialized = JsonSerializer.Deserialize<T>(json, options);
        Assert.NotNull(deserialized);
        return deserialized;
    }
}
