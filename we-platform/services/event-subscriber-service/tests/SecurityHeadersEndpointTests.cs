using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WePlatform.AspNetCore;

namespace EventSubscriberService.Tests;

public class SecurityHeadersEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityHeadersEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();
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
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }
}
