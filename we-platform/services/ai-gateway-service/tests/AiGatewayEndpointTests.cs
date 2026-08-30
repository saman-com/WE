using System.Net;
using System.Net.Http.Json;
using AiGatewayService.Application;

namespace AiGatewayService.Tests;

public class AiGatewayEndpointTests : IClassFixture<AiGatewayWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AiGatewayEndpointTests(AiGatewayWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PlatformService_CanCompleteAiRequest()
    {
        var serviceId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/ai/complete",
            serviceId,
            TestJwt.PlatformServiceRole);
        request.Content = JsonContent.Create(new AiCompletionRequest(
            "assessment-feedback",
            "1.0.0",
            "teacher-review",
            new Dictionary<string, string>
            {
                ["studentName"] = "Alex",
                ["microSkillName"] = "Fractions",
                ["evidenceSummary"] = "Submitted work shows partial understanding."
            }));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AiCompletionResponse>();
        Assert.NotNull(body);
        Assert.Equal("assessment-feedback", body.PromptId);
        Assert.Equal("1.0.0", body.PromptVersion);
        Assert.False(string.IsNullOrWhiteSpace(body.Content));
        Assert.True(body.SafetyPassed);
    }

    [Fact]
    public async Task Teacher_CannotCallCompleteEndpoint()
    {
        var teacherId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/ai/complete",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new AiCompletionRequest(
            "assessment-feedback",
            "1.0.0",
            "teacher-review",
            new Dictionary<string, string>()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ai/complete");
        request.Content = JsonContent.Create(new AiCompletionRequest(
            "assessment-feedback",
            "1.0.0",
            "teacher-review",
            new Dictionary<string, string>()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
