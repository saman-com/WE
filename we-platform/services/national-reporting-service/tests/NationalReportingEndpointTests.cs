using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NationalReportingService.Application;
using NationalReportingService.Domain;
using NationalReportingService.Infrastructure.Data;
using NationalReportingService.Infrastructure.Security;

namespace NationalReportingService.Tests;

public class NationalReportingEndpointTests : IClassFixture<NationalReportingWebApplicationFactory>
{
    private static readonly DateOnly AsOf = new(2026, 10, 1);
    private readonly HttpClient _client;
    private readonly NationalReportingWebApplicationFactory _factory;

    public NationalReportingEndpointTests(NationalReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task MinistryKey_GetsAggregatedEnrollment_WithoutStudentPii()
    {
        await ResetAndSeedApiKeysAsync();
        await SeedEnrollmentAsync("NORTH", "SCH-A", 120, 10);
        await SeedEnrollmentAsync("NORTH", "SCH-B", 80, 8);
        await SeedEnrollmentAsync("SOUTH", "SCH-C", 200, 15);

        using var request = TestApiKey.Authorized(
            HttpMethod.Get,
            "/api/v1/national/enrollment",
            TestApiKey.FullAccessKey);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<NationalEnrollmentResponse>();
        Assert.NotNull(report);
        Assert.Equal(3, report.TotalSchools);
        Assert.Equal(400, report.TotalStudents);
        Assert.Equal(33, report.TotalTeachers);
        Assert.Equal(AsOf, report.AsOfDate);
        Assert.Equal(2, report.Regions.Count);

        var north = report.Regions.Single(r => r.RegionCode == "NORTH");
        Assert.Equal(2, north.SchoolCount);
        Assert.Equal(200, north.StudentCount);

        var payload = await response.Content.ReadAsStringAsync();
        AssertNoStudentPii(payload);
    }

    [Fact]
    public async Task MinistryKey_GetsNationalMasteryBenchmarks_AggregatedCorrectly()
    {
        await ResetAndSeedApiKeysAsync();
        await SeedMasteryAsync("NORTH", "MATH", 70m, 60m, 100);
        await SeedMasteryAsync("SOUTH", "MATH", 80m, 70m, 100);
        await SeedMasteryAsync("NORTH", "SCI", 65m, 55m, 50);

        using var request = TestApiKey.Authorized(
            HttpMethod.Get,
            "/api/v1/national/mastery-benchmarks",
            TestApiKey.FullAccessKey);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<NationalMasteryBenchmarksResponse>();
        Assert.NotNull(report);
        Assert.Equal(AsOf, report.AsOfDate);

        var math = report.National.Single(b => b.SubjectCode == "MATH");
        Assert.Equal(75m, math.AverageMasteryPercent);
        Assert.Equal(65m, math.MasteredSharePercent);
        Assert.Equal(200, math.SampleSize);

        var payload = await response.Content.ReadAsStringAsync();
        AssertNoStudentPii(payload);
    }

    [Fact]
    public async Task MinistryKey_GetsCurriculumCoverage_AggregatedCorrectly()
    {
        await ResetAndSeedApiKeysAsync();
        await SeedCoverageAsync("NORTH", "NAT-MATH", 80m, 10);
        await SeedCoverageAsync("SOUTH", "NAT-MATH", 90m, 20);
        await SeedCoverageAsync("NORTH", "NAT-SCI", 70m, 5);

        using var request = TestApiKey.Authorized(
            HttpMethod.Get,
            "/api/v1/national/curriculum-coverage",
            TestApiKey.FullAccessKey);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<NationalCurriculumCoverageResponse>();
        Assert.NotNull(report);
        Assert.Equal(AsOf, report.AsOfDate);

        var math = report.National.Single(c => c.CurriculumCode == "NAT-MATH");
        Assert.Equal(30, math.SchoolsReporting);
        Assert.Equal(86.67m, Math.Round(math.CoveredObjectivePercent, 2));

        var payload = await response.Content.ReadAsStringAsync();
        AssertNoStudentPii(payload);
    }

    [Fact]
    public async Task MissingApiKey_IsUnauthorized()
    {
        await ResetAndSeedApiKeysAsync();

        var response = await _client.GetAsync("/api/v1/national/enrollment");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidApiKey_IsUnauthorized()
    {
        await ResetAndSeedApiKeysAsync();

        using var request = TestApiKey.Authorized(
            HttpMethod.Get,
            "/api/v1/national/enrollment",
            "not-a-valid-key");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EnrollmentOnlyKey_CannotAccessMasteryBenchmarks()
    {
        await ResetAndSeedApiKeysAsync();
        await SeedMasteryAsync("NORTH", "MATH", 70m, 60m, 100);

        using var request = TestApiKey.Authorized(
            HttpMethod.Get,
            "/api/v1/national/mastery-benchmarks",
            TestApiKey.EnrollmentOnlyKey);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InactiveApiKey_IsUnauthorized()
    {
        await ResetAndSeedApiKeysAsync();

        using var request = TestApiKey.Authorized(
            HttpMethod.Get,
            "/api/v1/national/enrollment",
            TestApiKey.InactiveKey);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocumentation_IsPublishedAtApiV1Docs()
    {
        var response = await _client.GetAsync("/api/v1/docs");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body));
        Assert.Contains("openapi", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NationalEndpoints_AreRateLimited()
    {
        await ResetAndSeedApiKeysAsync();
        await SeedEnrollmentAsync("NORTH", "SCH-A", 10, 1);

        HttpStatusCode? lastStatus = null;
        for (var i = 0; i < 5; i++)
        {
            using var request = TestApiKey.Authorized(
                HttpMethod.Get,
                "/api/v1/national/enrollment",
                TestApiKey.RateLimitKey);
            var response = await _client.SendAsync(request);
            lastStatus = response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                break;
            }
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    private async Task ResetAndSeedApiKeysAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();

        db.RegionalEnrollmentFacts.RemoveRange(db.RegionalEnrollmentFacts);
        db.RegionalMasteryFacts.RemoveRange(db.RegionalMasteryFacts);
        db.RegionalCoverageFacts.RemoveRange(db.RegionalCoverageFacts);
        db.MinistryApiKeys.RemoveRange(db.MinistryApiKeys);
        await db.SaveChangesAsync();

        db.MinistryApiKeys.AddRange(
            new MinistryApiKey
            {
                Id = Guid.CreateVersion7(),
                ClientName = "Full Access Ministry",
                KeyHash = ApiKeyHasher.Hash(TestApiKey.FullAccessKey),
                Scopes = TestApiKey.FullScopes,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new MinistryApiKey
            {
                Id = Guid.CreateVersion7(),
                ClientName = "Enrollment Only Ministry",
                KeyHash = ApiKeyHasher.Hash(TestApiKey.EnrollmentOnlyKey),
                Scopes = NationalApiScopes.Enrollment,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new MinistryApiKey
            {
                Id = Guid.CreateVersion7(),
                ClientName = "Inactive Ministry",
                KeyHash = ApiKeyHasher.Hash(TestApiKey.InactiveKey),
                Scopes = TestApiKey.FullScopes,
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new MinistryApiKey
            {
                Id = Guid.CreateVersion7(),
                ClientName = "Rate Limit Ministry",
                KeyHash = ApiKeyHasher.Hash(TestApiKey.RateLimitKey),
                Scopes = TestApiKey.FullScopes,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
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

    private async Task SeedCoverageAsync(
        string region,
        string curriculum,
        decimal coveredPercent,
        int schoolsReporting)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();
        db.RegionalCoverageFacts.Add(new RegionalCoverageFact
        {
            Id = Guid.CreateVersion7(),
            RegionCode = region,
            CurriculumCode = curriculum,
            CoveredObjectivePercent = coveredPercent,
            SchoolsReporting = schoolsReporting,
            AsOfDate = AsOf
        });
        await db.SaveChangesAsync();
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
