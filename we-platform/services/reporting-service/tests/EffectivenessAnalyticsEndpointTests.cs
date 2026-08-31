using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ReportingService.Application;
using ReportingService.Infrastructure.Data.Edw;
using WePlatform.Tenancy;

namespace ReportingService.Tests;

public class EffectivenessAnalyticsEndpointTests : IClassFixture<ReportingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly ReportingWebApplicationFactory _factory;

    public EffectivenessAnalyticsEndpointTests(ReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _factory = factory;
    }

    [Fact]
    public async Task SchoolLeader_CanViewEffectivenessAnalysis_WithKnownEdwSeedData()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var scienceSubjectId = Guid.CreateVersion7();
        var mathsSubjectId = Guid.CreateVersion7();
        var forcesUnitId = Guid.CreateVersion7();
        var algebraUnitId = Guid.CreateVersion7();
        var fractionsUnitId = Guid.CreateVersion7();

        _accessChecker.AllowSchoolLeader(leaderId, organisationId);
        await SeedEffectivenessEdwDataAsync(
            organisationId,
            scienceSubjectId,
            mathsSubjectId,
            forcesUnitId,
            algebraUnitId,
            fractionsUnitId);

        var response = await SendAsAsync<OrganisationEffectivenessResponse>(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/effectiveness",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(organisationId, response.OrganisationId);

        Assert.Equal(3, response.CurriculumEffectiveness.Count);

        var forces = response.CurriculumEffectiveness.Single(c => c.UnitId == forcesUnitId);
        Assert.Equal("Science", forces.SubjectName);
        Assert.Equal("Forces", forces.UnitName);
        Assert.Equal(0.75, forces.MasteryRate, precision: 2);
        Assert.Equal(8, forces.TotalMicroSkills);
        Assert.Equal(6, forces.MasteredMicroSkills);
        Assert.False(forces.IsUnderperforming);

        var algebra = response.CurriculumEffectiveness.Single(c => c.UnitId == algebraUnitId);
        Assert.Equal(0.40, algebra.MasteryRate, precision: 2);
        Assert.True(algebra.IsUnderperforming);

        var fractions = response.CurriculumEffectiveness.Single(c => c.UnitId == fractionsUnitId);
        Assert.Equal(0.50, fractions.MasteryRate, precision: 2);
        Assert.True(fractions.IsUnderperforming);

        Assert.Equal(2, response.InterventionEffectiveness.Count);

        var guidedPractice = response.InterventionEffectiveness.Single(i => i.InterventionType == "GuidedPractice");
        Assert.Equal(3, guidedPractice.TotalCount);
        Assert.Equal(2, guidedPractice.SuccessfulCount);
        Assert.Equal(0.67, guidedPractice.SuccessRate, precision: 2);

        var oneToOne = response.InterventionEffectiveness.Single(i => i.InterventionType == "OneToOne");
        Assert.Equal(2, oneToOne.TotalCount);
        Assert.Equal(1, oneToOne.SuccessfulCount);
        Assert.Equal(0.50, oneToOne.SuccessRate, precision: 2);
    }

    [Fact]
    public async Task SchoolLeader_CannotViewEffectivenessAnalysis_ForUnassignedOrganisation()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/effectiveness",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotViewEffectivenessAnalysis()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/effectiveness",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task SeedEffectivenessEdwDataAsync(
        Guid organisationId,
        Guid scienceSubjectId,
        Guid mathsSubjectId,
        Guid forcesUnitId,
        Guid algebraUnitId,
        Guid fractionsUnitId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EdwAnalyticsDbContext>();
        await db.Database.EnsureCreatedAsync();

        SeedTimeDimension(db, 20260401);

        db.EvidenceFacts.AddRange(
            CreateCurriculumEvidence(organisationId, scienceSubjectId, "Science", forcesUnitId, "Forces", 6, 8),
            CreateCurriculumEvidence(organisationId, mathsSubjectId, "Mathematics", algebraUnitId, "Algebra", 2, 5),
            CreateCurriculumEvidence(organisationId, mathsSubjectId, "Mathematics", fractionsUnitId, "Fractions", 3, 6));

        db.InterventionFacts.AddRange(
            CreateTypedIntervention(organisationId, "GuidedPractice", "Closed"),
            CreateTypedIntervention(organisationId, "GuidedPractice", "Closed"),
            CreateTypedIntervention(organisationId, "GuidedPractice", "Active"),
            CreateTypedIntervention(organisationId, "OneToOne", "Closed"),
            CreateTypedIntervention(organisationId, "OneToOne", "Active"));

        await db.SaveChangesAsync();
    }

    private static void SeedTimeDimension(EdwAnalyticsDbContext db, params int[] dateKeys)
    {
        foreach (var dateKey in dateKeys)
        {
            if (db.DimTimes.Any(d => d.DateKey == dateKey))
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

    private static EdwEvidenceFact CreateCurriculumEvidence(
        Guid organisationId,
        Guid subjectId,
        string subjectName,
        Guid unitId,
        string unitName,
        int masteredCount,
        int totalCount) =>
        new()
        {
            TenantId = organisationId,
            EventId = Guid.CreateVersion7(),
            EvidenceId = Guid.CreateVersion7(),
            OrganisationId = organisationId,
            AssessmentId = Guid.CreateVersion7(),
            SubmissionId = Guid.CreateVersion7(),
            StudentUserId = "student-effectiveness-1",
            ClassId = Guid.CreateVersion7(),
            ApprovedByTeacherUserId = "teacher-1",
            ApprovedAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            TimeKey = 20260401,
            MicroSkillCount = totalCount,
            MasteredMicroSkillCount = masteredCount,
            TotalMicroSkillCount = totalCount,
            SubjectId = subjectId,
            SubjectName = subjectName,
            UnitId = unitId,
            UnitName = unitName,
            IngestedAt = DateTimeOffset.UtcNow
        };

    private static EdwInterventionFact CreateTypedIntervention(
        Guid organisationId,
        string interventionType,
        string status) =>
        new()
        {
            TenantId = organisationId,
            EventId = Guid.CreateVersion7(),
            InterventionId = Guid.CreateVersion7(),
            OrganisationId = organisationId,
            StudentUserId = "student-effectiveness-1",
            LearningGapId = Guid.CreateVersion7(),
            AssignedTeacherUserId = "teacher-1",
            InterventionType = interventionType,
            Status = status,
            CreatedAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            TimeKey = 20260401,
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
