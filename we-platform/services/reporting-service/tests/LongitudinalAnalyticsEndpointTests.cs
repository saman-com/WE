using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReportingService.Application;
using ReportingService.Infrastructure.Data.Edw;
using WePlatform.Tenancy;

namespace ReportingService.Tests;

public class LongitudinalAnalyticsEndpointTests : IClassFixture<ReportingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly ReportingWebApplicationFactory _factory;

    public LongitudinalAnalyticsEndpointTests(ReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _factory = factory;
    }

    [Fact]
    public async Task Teacher_CanViewStudentLongitudinalAnalysis_WithMultiPeriodEdwData()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var studentUserId = "student-longitudinal-1";
        var gapId1 = Guid.CreateVersion7();
        var gapId2 = Guid.CreateVersion7();

        _accessChecker.AllowTeacherForStudent(teacherId, studentUserId);
        await SeedMultiPeriodEdwDataAsync(organisationId, studentUserId, gapId1, gapId2);

        var response = await SendAsAsync<StudentLongitudinalResponse>(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/students/{studentUserId}/longitudinal",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);

        Assert.Equal(organisationId, response.OrganisationId);
        Assert.Equal(studentUserId, response.StudentUserId);
        Assert.Equal(3, response.MasteryTrend.Count);

        Assert.Equal(202601, response.MasteryTrend[0].PeriodKey);
        Assert.Equal(3, response.MasteryTrend[0].MicroSkillsRecorded);
        Assert.Equal(3, response.MasteryTrend[0].CumulativeMicroSkills);

        Assert.Equal(202602, response.MasteryTrend[1].PeriodKey);
        Assert.Equal(5, response.MasteryTrend[1].MicroSkillsRecorded);
        Assert.Equal(8, response.MasteryTrend[1].CumulativeMicroSkills);

        Assert.Equal(202603, response.MasteryTrend[2].PeriodKey);
        Assert.Equal(2, response.MasteryTrend[2].MicroSkillsRecorded);
        Assert.Equal(10, response.MasteryTrend[2].CumulativeMicroSkills);

        Assert.Contains(response.GapHistory, e => e.LearningGapId == gapId1 && e.EventType == "Opened");
        Assert.Contains(response.GapHistory, e => e.LearningGapId == gapId2 && e.EventType == "Opened");
        Assert.Contains(response.GapHistory, e => e.LearningGapId == gapId1 && e.EventType == "Closed");

        Assert.Equal(2, response.InterventionOutcomes.Count);
        Assert.Contains(response.InterventionOutcomes, i => i.LearningGapId == gapId1 && i.Status == "Closed");
        Assert.Contains(response.InterventionOutcomes, i => i.LearningGapId == gapId2 && i.Status == "Active");
    }

    [Fact]
    public async Task Teacher_CannotViewLongitudinalAnalysis_ForUnassignedStudent()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var studentUserId = "student-unassigned";

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/students/{studentUserId}/longitudinal",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CanViewOrganisationLongitudinalAnalysis_WithMultiPeriodEdwData()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var student1 = "student-org-1";
        var student2 = "student-org-2";

        _accessChecker.AllowSchoolLeader(leaderId, organisationId);
        await SeedMultiPeriodEdwDataAsync(organisationId, student1, Guid.CreateVersion7(), Guid.CreateVersion7());
        await SeedSinglePeriodEvidenceAsync(organisationId, student2, 202602, 4);

        var response = await SendAsAsync<OrganisationLongitudinalResponse>(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/longitudinal",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(organisationId, response.OrganisationId);
        Assert.Equal(2, response.StudentSummaries.Count);

        var summary1 = response.StudentSummaries.Single(s => s.StudentUserId == student1);
        Assert.Equal(10, summary1.CumulativeMicroSkills);
        Assert.Equal(2, summary1.InterventionCount);

        var summary2 = response.StudentSummaries.Single(s => s.StudentUserId == student2);
        Assert.Equal(4, summary2.CumulativeMicroSkills);

        Assert.Equal(3, response.SchoolMasteryTrend.Count);
        Assert.Equal(202601, response.SchoolMasteryTrend[0].PeriodKey);
        Assert.Equal(202602, response.SchoolMasteryTrend[1].PeriodKey);
        Assert.Equal(12, response.SchoolMasteryTrend[1].CumulativeMicroSkills);
        Assert.Equal(14, response.SchoolMasteryTrend[2].CumulativeMicroSkills);
    }

    [Fact]
    public async Task SchoolLeader_OrganisationLongitudinalAnalysis_ReturnsEmptyCollections_WhenNoEdwFacts()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        _accessChecker.AllowSchoolLeader(leaderId, organisationId);

        var response = await SendAsAsync<OrganisationLongitudinalResponse>(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/longitudinal",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(organisationId, response.OrganisationId);
        Assert.Empty(response.StudentSummaries);
        Assert.Empty(response.SchoolMasteryTrend);
    }

    [Fact]
    public async Task SchoolLeader_CannotViewOrganisationLongitudinalAnalysis_ForUnassignedOrganisation()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/longitudinal",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotViewLongitudinalAnalysis()
    {
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/students/{studentId}/longitudinal",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task SeedMultiPeriodEdwDataAsync(
        Guid organisationId,
        string studentUserId,
        Guid gapId1,
        Guid gapId2)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EdwAnalyticsDbContext>();
        await db.Database.EnsureCreatedAsync();

        SeedTimeDimension(db, 20260115, 20260220, 20260310);

        db.EvidenceFacts.AddRange(
            CreateEvidence(organisationId, studentUserId, 20260115, 3),
            CreateEvidence(organisationId, studentUserId, 20260220, 5),
            CreateEvidence(organisationId, studentUserId, 20260310, 2));

        db.InterventionFacts.AddRange(
            CreateIntervention(organisationId, studentUserId, gapId1, "Closed", 20260201),
            CreateIntervention(organisationId, studentUserId, gapId2, "Active", 20260305));

        await db.SaveChangesAsync();
    }

    private async Task SeedSinglePeriodEvidenceAsync(
        Guid organisationId,
        string studentUserId,
        int periodKey,
        int microSkillCount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EdwAnalyticsDbContext>();
        await db.Database.EnsureCreatedAsync();

        var year = periodKey / 100;
        var month = periodKey % 100;
        var dateKey = year * 10_000 + month * 100 + 15;
        SeedTimeDimension(db, dateKey);

        db.EvidenceFacts.Add(CreateEvidence(organisationId, studentUserId, dateKey, microSkillCount));
        await db.SaveChangesAsync();
    }

    private static void SeedTimeDimension(EdwAnalyticsDbContext db, params int[] dateKeys)
    {
        foreach (var dateKey in dateKeys)
        {
            if (db.DimTimes.IgnoreQueryFilters().Any(d => d.DateKey == dateKey))
            {
                continue;
            }

            var year = dateKey / 10_000;
            var month = (dateKey / 100) % 100;
            var day = dateKey % 100;
            db.DimTimes.Add(new EdwDimTime
            {
                TenantId = DefaultTenant.Id,
                DateKey = dateKey,
                CalendarDate = new DateOnly(year, month, day),
                Year = year,
                Month = month,
                Day = day
            });
        }
    }

    private static EdwEvidenceFact CreateEvidence(
        Guid organisationId,
        string studentUserId,
        int dateKey,
        int microSkillCount) =>
        new()
        {
            TenantId = organisationId,
            EventId = Guid.CreateVersion7(),
            EvidenceId = Guid.CreateVersion7(),
            OrganisationId = organisationId,
            AssessmentId = Guid.CreateVersion7(),
            SubmissionId = Guid.CreateVersion7(),
            StudentUserId = studentUserId,
            ClassId = Guid.CreateVersion7(),
            ApprovedByTeacherUserId = "teacher-1",
            ApprovedAt = new DateTimeOffset(2026, dateKey / 100 % 100, dateKey % 100, 0, 0, 0, TimeSpan.Zero),
            TimeKey = dateKey,
            MicroSkillCount = microSkillCount,
            IngestedAt = DateTimeOffset.UtcNow
        };

    private static EdwInterventionFact CreateIntervention(
        Guid organisationId,
        string studentUserId,
        Guid gapId,
        string status,
        int dateKey) =>
        new()
        {
            TenantId = organisationId,
            EventId = Guid.CreateVersion7(),
            InterventionId = Guid.CreateVersion7(),
            OrganisationId = organisationId,
            StudentUserId = studentUserId,
            LearningGapId = gapId,
            AssignedTeacherUserId = "teacher-1",
            Status = status,
            CreatedAt = new DateTimeOffset(2026, dateKey / 100 % 100, dateKey % 100, 0, 0, 0, TimeSpan.Zero),
            TimeKey = dateKey,
            IngestedAt = DateTimeOffset.UtcNow
        };

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
}
