using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WePlatform.AspNetCore;

namespace SampleService.Tests;

public class SecurityHeadersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityHeadersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsBaselineSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            SecurityHeadersMiddleware.ContentTypeOptions,
            response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(
            SecurityHeadersMiddleware.FrameOptions,
            response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal(
            SecurityHeadersMiddleware.ReferrerPolicy,
            response.Headers.GetValues("Referrer-Policy").Single());
    }
}
