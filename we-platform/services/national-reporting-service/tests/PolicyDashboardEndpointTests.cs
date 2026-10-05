using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NationalReportingService.Application;
using NationalReportingService.Domain;
using NationalReportingService.Infrastructure.Data;

namespace NationalReportingService.Tests;

public class PolicyDashboardEndpointTests : IClassFixture<NationalReportingWebApplicationFactory>
{
    private static readonly DateOnly AsOf = new(2026, 10, 1);
    private readonly HttpClient _client;
    private readonly NationalReportingWebApplicationFactory _factory;

    public PolicyDashboardEndpointTests(NationalReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task EducationAuthorityOfficer_GetsNationalRegionalAggregatedTrends()
    {
        await ResetFactsAsync();
        await SeedEnrollmentAsync("NORTH", "SCH-A", 120, 10);
        await SeedEnrollmentAsync("NORTH", "SCH-B", 80, 8);
        await SeedEnrollmentAsync("SOUTH", "SCH-C", 200, 15);
        await SeedMasteryAsync("NORTH", "MATH", 70m, 60m, 100);
        await SeedMasteryAsync("SOUTH", "MATH", 80m, 70m, 100);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/trends",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<PolicyTrendsResponse>();
        Assert.NotNull(report);
        Assert.Equal(AsOf, report.AsOfDate);
        Assert.Equal(3, report.TotalSchools);
        AssertVisibleCount(400, report.TotalStudents);
        Assert.Equal(75m, report.NationalAverageMasteryPercent);
        Assert.Equal(2, report.Regions.Count);

        var north = report.Regions.Single(r => r.RegionCode == "NORTH");
        Assert.Equal(2, north.SchoolCount);
        AssertVisibleCount(200, north.StudentCount);
        Assert.Equal(70m, north.AverageMasteryPercent);

        AssertNoStudentPii(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Trends_SuppressesSmallRegionCounts_AndTotalsCannotRecoverHiddenValues()
    {
        await ResetFactsAsync();
        await SeedEnrollmentAsync("TINY", "SCH-T", 3, 2);
        await SeedEnrollmentAsync("LARGE", "SCH-L", 100, 10);
        await SeedMasteryAsync("TINY", "MATH", 90m, 80m, 3);
        await SeedMasteryAsync("LARGE", "MATH", 70m, 60m, 100);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/trends",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<PolicyTrendsResponse>();
        Assert.NotNull(report);

        var tiny = report.Regions.Single(r => r.RegionCode == "TINY");
        AssertSuppressed(tiny.StudentCount);
        Assert.Null(tiny.AverageMasteryPercent);

        var large = report.Regions.Single(r => r.RegionCode == "LARGE");
        AssertVisibleCount(100, large.StudentCount);
        Assert.Equal(70m, large.AverageMasteryPercent);

        AssertSuppressed(report.TotalStudents);
        Assert.Null(report.NationalAverageMasteryPercent);
    }

    [Fact]
    public async Task EducationAuthorityOfficer_GetsEquityAnalysis_MasteryByDemographicAndRegion()
    {
        await ResetFactsAsync();
        await SeedEquityAsync("NORTH", "Gender", "Female", 78m, 50);
        await SeedEquityAsync("NORTH", "Gender", "Male", 72m, 50);
        await SeedEquityAsync("SOUTH", "Gender", "Female", 81m, 40);
        await SeedEquityAsync("SOUTH", "SocioEconomic", "Low", 65m, 60);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/equity",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<EquityAnalysisResponse>();
        Assert.NotNull(report);
        Assert.Equal(AsOf, report.AsOfDate);
        Assert.Equal(4, report.Distributions.Count);

        var northFemale = report.Distributions.Single(d =>
            d.RegionCode == "NORTH"
            && d.DemographicDimension == "Gender"
            && d.DemographicCategory == "Female");
        Assert.Equal(78m, northFemale.AverageMasteryPercent);
        AssertVisibleCount(50, northFemale.SampleSize);

        AssertNoStudentPii(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Equity_SuppressesSmallDemographicSamples_AndNullsRelatedPercentages()
    {
        await ResetFactsAsync();
        await SeedEquityAsync("TINY", "Gender", "Female", 95m, 2);
        await SeedEquityAsync("LARGE", "Gender", "Female", 78m, 50);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/equity",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<EquityAnalysisResponse>();
        Assert.NotNull(report);

        var tiny = report.Distributions.Single(d => d.RegionCode == "TINY");
        AssertSuppressed(tiny.SampleSize);
        Assert.Null(tiny.AverageMasteryPercent);

        var large = report.Distributions.Single(d => d.RegionCode == "LARGE");
        AssertVisibleCount(50, large.SampleSize);
        Assert.Equal(78m, large.AverageMasteryPercent);
    }

    [Fact]
    public async Task EducationAuthorityOfficer_GetsCurriculumEffectiveness_ComparedAcrossRegions()
    {
        await ResetFactsAsync();
        await SeedCurriculumEffectivenessAsync("NORTH", "NAT-MATH", "MATH", 70m, 80m, 10);
        await SeedCurriculumEffectivenessAsync("SOUTH", "NAT-MATH", "MATH", 85m, 90m, 20);
        await SeedCurriculumEffectivenessAsync("NORTH", "NAT-SCI", "SCI", 60m, 70m, 5);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/curriculum-effectiveness",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<CurriculumEffectivenessComparisonResponse>();
        Assert.NotNull(report);
        Assert.Equal(AsOf, report.AsOfDate);
        Assert.Equal(3, report.Regions.Count);

        var southMath = report.Regions.Single(r =>
            r.RegionCode == "SOUTH" && r.CurriculumCode == "NAT-MATH");
        Assert.Equal(85m, southMath.MasteryRatePercent);
        Assert.Equal(90m, southMath.CoveragePercent);
        AssertVisibleCount(20, southMath.SchoolsReporting);

        AssertNoStudentPii(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CurriculumEffectiveness_SuppressesSmallSchoolGroups_AndNullsRelatedPercentages()
    {
        await ResetFactsAsync();
        await SeedCurriculumEffectivenessAsync("TINY", "NAT-MATH", "MATH", 95m, 90m, 2);
        await SeedCurriculumEffectivenessAsync("LARGE", "NAT-MATH", "MATH", 85m, 90m, 20);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/curriculum-effectiveness",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<CurriculumEffectivenessComparisonResponse>();
        Assert.NotNull(report);

        var tiny = report.Regions.Single(r => r.RegionCode == "TINY");
        AssertSuppressed(tiny.SchoolsReporting);
        Assert.Null(tiny.MasteryRatePercent);
        Assert.Null(tiny.CoveragePercent);

        var large = report.Regions.Single(r => r.RegionCode == "LARGE");
        AssertVisibleCount(20, large.SchoolsReporting);
        Assert.Equal(85m, large.MasteryRatePercent);
    }

    [Fact]
    public async Task EducationAuthorityOfficer_GetsInterventionImpactAtScale()
    {
        await ResetFactsAsync();
        await SeedInterventionImpactAsync("NORTH", "TargetedTutoring", 40, 30, 12.5m);
        await SeedInterventionImpactAsync("SOUTH", "TargetedTutoring", 60, 45, 10m);
        await SeedInterventionImpactAsync("NORTH", "SmallGroup", 20, 10, 8m);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/intervention-impact",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<InterventionImpactResponse>();
        Assert.NotNull(report);
        Assert.Equal(AsOf, report.AsOfDate);
        Assert.Equal(3, report.Regions.Count);

        var northTutoring = report.Regions.Single(r =>
            r.RegionCode == "NORTH" && r.InterventionType == "TargetedTutoring");
        AssertVisibleCount(40, northTutoring.TotalCount);
        AssertVisibleCount(30, northTutoring.SuccessfulCount);
        Assert.Equal(75m, northTutoring.SuccessRatePercent);
        Assert.Equal(12.5m, northTutoring.AverageGrowthPercent);

        AssertNoStudentPii(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InterventionImpact_SuppressesSmallCounts_AndNullsRelatedPercentages()
    {
        await ResetFactsAsync();
        await SeedInterventionImpactAsync("TINY", "TargetedTutoring", 3, 2, 15m);
        await SeedInterventionImpactAsync("LARGE", "TargetedTutoring", 40, 30, 12.5m);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/intervention-impact",
            Guid.NewGuid().ToString(),
            TestJwt.EducationAuthorityOfficerRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<InterventionImpactResponse>();
        Assert.NotNull(report);

        var tiny = report.Regions.Single(r => r.RegionCode == "TINY");
        AssertSuppressed(tiny.TotalCount);
        AssertSuppressed(tiny.SuccessfulCount);
        Assert.Null(tiny.SuccessRatePercent);
        Assert.Null(tiny.AverageGrowthPercent);

        var large = report.Regions.Single(r => r.RegionCode == "LARGE");
        AssertVisibleCount(40, large.TotalCount);
        AssertVisibleCount(30, large.SuccessfulCount);
        Assert.Equal(75m, large.SuccessRatePercent);
    }

    [Fact]
    public async Task Teacher_CannotAccessPolicyDashboards()
    {
        await ResetFactsAsync();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/trends",
            Guid.NewGuid().ToString(),
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SchoolLeader_CannotAccessPolicyDashboards()
    {
        await ResetFactsAsync();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/equity",
            Guid.NewGuid().ToString(),
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CannotAccessPolicyDashboards()
    {
        await ResetFactsAsync();

        var response = await _client.GetAsync("/api/v1/policy-dashboards/trends");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MinistryApiKey_CannotAccessPolicyDashboards()
    {
        await ResetFactsAsync();

        using var request = TestApiKey.Authorized(
            HttpMethod.Get,
            "/api/v1/policy-dashboards/trends",
            TestApiKey.FullAccessKey);
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected Unauthorized or Forbidden, got {response.StatusCode}");
    }

    private async Task ResetFactsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();

        db.RegionalEnrollmentFacts.RemoveRange(db.RegionalEnrollmentFacts);
        db.RegionalMasteryFacts.RemoveRange(db.RegionalMasteryFacts);
        db.RegionalCoverageFacts.RemoveRange(db.RegionalCoverageFacts);
        db.RegionalEquityFacts.RemoveRange(db.RegionalEquityFacts);
        db.RegionalCurriculumEffectivenessFacts.RemoveRange(db.RegionalCurriculumEffectivenessFacts);
        db.RegionalInterventionImpactFacts.RemoveRange(db.RegionalInterventionImpactFacts);
        await db.SaveChangesAsync();
    }

    private async Task SeedEnrollmentAsync(string region, string school, int students, int teachers)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
        db.RegionalEnrollmentFacts.Add(new RegionalEnrollmentFact
        {
            Id = Guid.CreateVersion7(),
            RegionCode = region,
            SchoolCode = school,
            StudentCount = students,
            TeacherCount = teachers,
            AsOfDate = AsOf
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedMasteryAsync(
        string region,
        string subject,
        decimal averageMastery,
        decimal masteredShare,
        int sampleSize)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
        db.RegionalMasteryFacts.Add(new RegionalMasteryFact
        {
            Id = Guid.CreateVersion7(),
            RegionCode = region,
            SubjectCode = subject,
            AverageMasteryPercent = averageMastery,
            MasteredSharePercent = masteredShare,
            SampleSize = sampleSize,
            AsOfDate = AsOf
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedEquityAsync(
        string region,
        string dimension,
        string category,
        decimal averageMastery,
        int sampleSize)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
        db.RegionalEquityFacts.Add(new RegionalEquityFact
        {
            Id = Guid.CreateVersion7(),
            RegionCode = region,
            DemographicDimension = dimension,
            DemographicCategory = category,
            AverageMasteryPercent = averageMastery,
            SampleSize = sampleSize,
            AsOfDate = AsOf
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedCurriculumEffectivenessAsync(
        string region,
        string curriculum,
        string subject,
        decimal masteryRate,
        decimal coverage,
        int schoolsReporting)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
        db.RegionalCurriculumEffectivenessFacts.Add(new RegionalCurriculumEffectivenessFact
        {
            Id = Guid.CreateVersion7(),
            RegionCode = region,
            CurriculumCode = curriculum,
            SubjectCode = subject,
            MasteryRatePercent = masteryRate,
            CoveragePercent = coverage,
            SchoolsReporting = schoolsReporting,
            AsOfDate = AsOf
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedInterventionImpactAsync(
        string region,
        string interventionType,
        int totalCount,
        int successfulCount,
        decimal averageGrowth)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
        db.RegionalInterventionImpactFacts.Add(new RegionalInterventionImpactFact
        {
            Id = Guid.CreateVersion7(),
            RegionCode = region,
            InterventionType = interventionType,
            TotalCount = totalCount,
            SuccessfulCount = successfulCount,
            AverageGrowthPercent = averageGrowth,
            AsOfDate = AsOf
        });
        await db.SaveChangesAsync();
    }

    private static void AssertVisibleCount(int expected, CountCell cell)
    {
        Assert.False(cell.Suppressed);
        Assert.Equal(expected, cell.Value);
    }

    private static void AssertSuppressed(CountCell cell)
    {
        Assert.True(cell.Suppressed);
        Assert.Null(cell.Value);
    }

    private static void AssertNoStudentPii(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        AssertNoForbiddenPropertyNames(document.RootElement);

        Assert.DoesNotContain("studentUserId", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", payload);
        Assert.DoesNotContain("firstName", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lastName", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoForbiddenPropertyNames(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name;
                    Assert.False(
                        name.Contains("student", StringComparison.OrdinalIgnoreCase)
                        && !name.Equals("studentCount", StringComparison.OrdinalIgnoreCase)
                        && !name.Equals("totalStudents", StringComparison.OrdinalIgnoreCase),
                        $"Response contains forbidden property '{name}'.");
                    AssertNoForbiddenPropertyNames(property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AssertNoForbiddenPropertyNames(item);
                }

                break;
        }
    }
}
