using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using NationalReportingService.Domain;
using NationalReportingService.Infrastructure.Data;
using NationalReportingService.Infrastructure.Security;

namespace NationalReportingService.Tests;

/// <summary>
/// Phase 8 performance validation: API responses under concurrent load must meet the 500ms SLA.
/// </summary>
public class PlatformSlaLoadTests : IClassFixture<NationalReportingWebApplicationFactory>
{
    private const int ConcurrentRequests = 25;
    private const int ApiSlaMilliseconds = 500;

    private static readonly DateOnly AsOf = new(2026, 10, 1);
    private readonly HttpClient _client;
    private readonly NationalReportingWebApplicationFactory _factory;

    public PlatformSlaLoadTests(NationalReportingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task NationalEnrollmentApi_UnderConcurrentLoad_Meets500msSla()
    {
        var apiKeys = await ResetAndSeedAsync(ConcurrentRequests + 1);

        // Warm-up (exclude host startup from SLA sample)
        using (var warm = TestApiKey.Authorized(
                   HttpMethod.Get,
                   "/api/v1/national/enrollment",
                   apiKeys[0]))
        {
            var warmResponse = await _client.SendAsync(warm);
            warmResponse.EnsureSuccessStatusCode();
        }

        var latencies = new long[ConcurrentRequests];
        var tasks = Enumerable.Range(0, ConcurrentRequests).Select(async i =>
        {
            // Unique API keys avoid fixed-window rate-limit partitions masking latency.
            var sw = Stopwatch.StartNew();
            using var request = TestApiKey.Authorized(
                HttpMethod.Get,
                "/api/v1/national/enrollment",
                apiKeys[i + 1]);
            var response = await _client.SendAsync(request);
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            latencies[i] = sw.ElapsedMilliseconds;
        });

        await Task.WhenAll(tasks);

        var maxLatency = latencies.Max();
        var p95 = Percentile(latencies, 0.95);
        Assert.True(
            maxLatency <= ApiSlaMilliseconds,
            $"National enrollment API max latency {maxLatency}ms exceeded {ApiSlaMilliseconds}ms SLA (p95={p95}ms).");
    }

    [Fact]
    public async Task PolicyDashboardTrendsApi_UnderConcurrentLoad_Meets500msSla()
    {
        await ResetAndSeedAsync(apiKeyCount: 0);

        using (var warm = TestJwt.Authorized(
                   HttpMethod.Get,
                   "/api/v1/policy-dashboards/trends",
                   Guid.NewGuid().ToString(),
                   TestJwt.EducationAuthorityOfficerRole))
        {
            var warmResponse = await _client.SendAsync(warm);
            warmResponse.EnsureSuccessStatusCode();
        }

        var latencies = new long[ConcurrentRequests];
        var tasks = Enumerable.Range(0, ConcurrentRequests).Select(async i =>
        {
            var sw = Stopwatch.StartNew();
            using var request = TestJwt.Authorized(
                HttpMethod.Get,
                "/api/v1/policy-dashboards/trends",
                Guid.NewGuid().ToString(),
                TestJwt.EducationAuthorityOfficerRole);
            var response = await _client.SendAsync(request);
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            latencies[i] = sw.ElapsedMilliseconds;
        });

        await Task.WhenAll(tasks);

        var maxLatency = latencies.Max();
        Assert.True(
            maxLatency <= ApiSlaMilliseconds,
            $"Policy trends API max latency {maxLatency}ms exceeded {ApiSlaMilliseconds}ms SLA.");
    }

    private static long Percentile(long[] values, double percentile)
    {
        var ordered = values.OrderBy(v => v).ToArray();
        var index = (int)Math.Ceiling(percentile * ordered.Length) - 1;
        return ordered[Math.Clamp(index, 0, ordered.Length - 1)];
    }

    private async Task<IReadOnlyList<string>> ResetAndSeedAsync(int apiKeyCount)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NationalReportingDbContext>();

        db.RegionalEnrollmentFacts.RemoveRange(db.RegionalEnrollmentFacts);
        db.RegionalMasteryFacts.RemoveRange(db.RegionalMasteryFacts);
        db.MinistryApiKeys.RemoveRange(db.MinistryApiKeys);
        await db.SaveChangesAsync();

        for (var i = 0; i < 40; i++)
        {
            db.RegionalEnrollmentFacts.Add(new RegionalEnrollmentFact
            {
                Id = Guid.CreateVersion7(),
                RegionCode = i % 2 == 0 ? "NORTH" : "SOUTH",
                SchoolCode = $"SCH-{i:D3}",
                StudentCount = 100 + i,
                TeacherCount = 10,
                AsOfDate = AsOf
            });
            db.RegionalMasteryFacts.Add(new RegionalMasteryFact
            {
                Id = Guid.CreateVersion7(),
                RegionCode = i % 2 == 0 ? "NORTH" : "SOUTH",
                SubjectCode = "MATH",
                AverageMasteryPercent = 70m + (i % 10),
                MasteredSharePercent = 60m,
                SampleSize = 100,
                AsOfDate = AsOf
            });
        }

        var apiKeys = new List<string>(apiKeyCount);
        for (var i = 0; i < apiKeyCount; i++)
        {
            var key = $"sla-load-key-{i:D3}-{Guid.NewGuid():N}";
            apiKeys.Add(key);
            db.MinistryApiKeys.Add(new MinistryApiKey
            {
                Id = Guid.CreateVersion7(),
                ClientName = $"SLA Load Ministry {i}",
                KeyHash = ApiKeyHasher.Hash(key),
                Scopes = TestApiKey.FullScopes,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return apiKeys;
    }
}
