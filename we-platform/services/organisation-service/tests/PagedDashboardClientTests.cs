using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OrganisationService.Infrastructure.Assessment;
using OrganisationService.Infrastructure.Evidence;
using OrganisationService.Infrastructure.StudentLearning;

namespace OrganisationService.Tests;

public sealed class PagedDashboardClientTests
{
    [Fact]
    public async Task AssessmentClient_FollowsCursor_UntilAllStudentSummariesLoaded()
    {
        var handler = new ScriptedHandler();
        var all = Enumerable.Range(0, 120).Select(i => new
        {
            id = Guid.Parse($"00000000-0000-4000-8000-{i:D12}"),
            title = $"Assessment {i}",
            dueAt = (DateTimeOffset?)null,
            learningObjectiveIds = Array.Empty<Guid>(),
            hasSubmitted = i % 2 == 0,
            submittedAt = (DateTimeOffset?)null
        }).ToList();

        handler.EnqueuePaged("/api/v1/assessments/student-summary", all, pageSize: 100);

        var client = new HttpAssessmentDashboardClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://assessment.test") },
            Config(("Assessment:BaseUrl", "http://assessment.test")),
            NullLogger<HttpAssessmentDashboardClient>.Instance);

        var result = await client.ListStudentAssessmentSummariesAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token");

        Assert.Equal(120, result.Count);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task EvidenceClient_FollowsCursor_UntilAllFeedbackLoaded()
    {
        var handler = new ScriptedHandler();
        var all = Enumerable.Range(0, 120).Select(i => new
        {
            id = Guid.Parse($"00000000-0000-4000-8000-{i:D12}"),
            assessmentId = Guid.Parse($"00000000-0000-4000-8001-{i:D12}"),
            title = $"Evidence {i}",
            approvedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
            microSkillMarks = Array.Empty<object>()
        }).ToList();

        handler.EnqueuePaged("/api/v1/evidence/student-feedback", all, pageSize: 100);

        var client = new HttpEvidenceDashboardClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://evidence.test") },
            Config(("Evidence:BaseUrl", "http://evidence.test")),
            NullLogger<HttpEvidenceDashboardClient>.Instance);

        var result = await client.ListStudentFeedbackAsync("token");

        Assert.Equal(120, result.Count);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task ProfileClient_FollowsCursor_UntilAllTimelineEntriesLoaded()
    {
        var handler = new ScriptedHandler();
        var studentId = "student-120";
        var all = Enumerable.Range(0, 120).Select(i => new
        {
            id = Guid.Parse($"00000000-0000-4000-8000-{i:D12}"),
            title = $"Entry {i}",
            recordedAt = DateTimeOffset.UtcNow.AddMinutes(-i)
        }).ToList();

        handler.EnqueueProfilePages(studentId, all, pageSize: 100);

        var client = new HttpStudentLearningProfileClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://learning.test") },
            Config(("StudentLearning:BaseUrl", "http://learning.test")),
            NullLogger<HttpStudentLearningProfileClient>.Instance);

        var result = await client.GetProfileAsync(studentId, "token");

        Assert.NotNull(result);
        Assert.Equal(120, result!.EvidenceTimeline.Count);
        Assert.Equal(2, handler.RequestCount);
    }

    private static IConfiguration Config(params (string Key, string Value)[] pairs)
    {
        var values = pairs.ToDictionary(p => p.Key, p => (string?)p.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static string? QueryValue(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2
                && string.Equals(Uri.UnescapeDataString(pair[0]), key, StringComparison.OrdinalIgnoreCase))
            {
                var value = Uri.UnescapeDataString(pair[1]);
                return string.IsNullOrEmpty(value) ? null : value;
            }
        }

        return null;
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
        public int RequestCount { get; private set; }

        public void EnqueuePaged<T>(string pathPrefix, IReadOnlyList<T> all, int pageSize)
        {
            var pages = (int)Math.Ceiling(all.Count / (double)pageSize);
            for (var page = 1; page <= pages; page++)
            {
                var pageNumber = page;
                var slice = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                var hasMore = pageNumber < pages;
                var expectedCursor = pageNumber == 1 ? null : pageNumber.ToString();
                var nextCursor = hasMore ? (pageNumber + 1).ToString() : null;
                _responses.Enqueue(request =>
                {
                    Assert.Contains(pathPrefix, request.RequestUri!.AbsoluteUri);
                    Assert.Equal(expectedCursor, QueryValue(request.RequestUri, "cursor"));
                    var body = new
                    {
                        items = slice,
                        hasMore,
                        nextCursor
                    };
                    return Json(body);
                });
            }
        }

        public void EnqueueProfilePages<T>(string studentId, IReadOnlyList<T> timeline, int pageSize)
        {
            var pages = (int)Math.Ceiling(timeline.Count / (double)pageSize);
            for (var page = 1; page <= pages; page++)
            {
                var pageNumber = page;
                var slice = timeline.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                var hasMore = pageNumber < pages;
                var expectedCursor = pageNumber == 1 ? null : pageNumber.ToString();
                var nextCursor = hasMore ? (pageNumber + 1).ToString() : null;
                _responses.Enqueue(request =>
                {
                    Assert.Contains($"/api/v1/students/{studentId}/profile", request.RequestUri!.AbsoluteUri);
                    Assert.Equal(expectedCursor, QueryValue(request.RequestUri, "cursor"));
                    var body = new
                    {
                        studentUserId = studentId,
                        enrollments = Array.Empty<object>(),
                        evidenceTimeline = slice,
                        hasMore,
                        nextCursor
                    };
                    return Json(body);
                });
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.True(_responses.Count > 0, $"Unexpected request: {request.RequestUri}");
            return Task.FromResult(_responses.Dequeue()(request));
        }

        private static HttpResponseMessage Json(object body) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
    }
}
