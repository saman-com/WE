using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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

        // Seed arithmetic: mastered 6+2+3=11 over total 8+5+6=19 → school avg ≈ 0.5789
        const int seededMastered = 6 + 2 + 3;
        const int seededTotal = 8 + 5 + 6;
        Assert.Equal(seededMastered, response.CurriculumEffectiveness.Sum(c => c.MasteredMicroSkills));
        Assert.Equal(seededTotal, response.CurriculumEffectiveness.Sum(c => c.TotalMicroSkills));
        var schoolAverage = (double)seededMastered / seededTotal;

        var forces = response.CurriculumEffectiveness.Single(c => c.UnitId == forcesUnitId);
        Assert.Equal("Science", forces.SubjectName);
        Assert.Equal("Forces", forces.UnitName);
        Assert.Equal(6.0 / 8.0, forces.MasteryRate, precision: 4);
        Assert.Equal(8, forces.TotalMicroSkills);
        Assert.Equal(6, forces.MasteredMicroSkills);
        Assert.False(forces.IsUnderperforming);
        Assert.True(forces.MasteryRate >= schoolAverage);

        var algebra = response.CurriculumEffectiveness.Single(c => c.UnitId == algebraUnitId);
        Assert.Equal(2.0 / 5.0, algebra.MasteryRate, precision: 4);
        Assert.True(algebra.IsUnderperforming);
        Assert.True(algebra.MasteryRate < schoolAverage);

        var fractions = response.CurriculumEffectiveness.Single(c => c.UnitId == fractionsUnitId);
        Assert.Equal(3.0 / 6.0, fractions.MasteryRate, precision: 4);
        Assert.True(fractions.IsUnderperforming);
        Assert.True(fractions.MasteryRate < schoolAverage);

        Assert.Equal(2, response.InterventionEffectiveness.Count);

        // GuidedPractice: 2 Closed of 3; OneToOne: 1 Closed of 2
        var guidedPractice = response.InterventionEffectiveness.Single(i => i.InterventionType == "GuidedPractice");
        Assert.Equal(3, guidedPractice.TotalCount);
        Assert.Equal(2, guidedPractice.SuccessfulCount);
        Assert.Equal(2.0 / 3.0, guidedPractice.SuccessRate, precision: 2);

        var oneToOne = response.InterventionEffectiveness.Single(i => i.InterventionType == "OneToOne");
        Assert.Equal(2, oneToOne.TotalCount);
        Assert.Equal(1, oneToOne.SuccessfulCount);
        Assert.Equal(1.0 / 2.0, oneToOne.SuccessRate, precision: 2);
        Assert.Equal(5, response.InterventionEffectiveness.Sum(i => i.TotalCount));
        Assert.Equal(3, response.InterventionEffectiveness.Sum(i => i.SuccessfulCount));
    }

    [Fact]
    public async Task SchoolLeader_EffectivenessAnalysis_ReturnsEmptyCollections_WhenNoEdwFacts()
    {
        var leaderId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();

        _accessChecker.AllowSchoolLeader(leaderId, organisationId);

        var response = await SendAsAsync<OrganisationEffectivenessResponse>(
            HttpMethod.Get,
            $"/api/v1/analytics/organisations/{organisationId}/effectiveness",
            leaderId,
            organisationId,
            TestJwt.SchoolLeaderRole);

        Assert.Equal(organisationId, response.OrganisationId);
        Assert.Empty(response.CurriculumEffectiveness);
        Assert.Empty(response.InterventionEffectiveness);
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
