using System.Net;
using System.Net.Http.Json;
using CommunicationService.Application;
using CommunicationService.Domain;

namespace CommunicationService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<CommunicationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(CommunicationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task Parent_FromDifferentTenant_CannotReadConversation()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentId, studentId);
        _accessChecker.AllowTeacher(teacherId, studentId);

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            tenantB,
            TestJwt.ParentRole,
            new SendMessageRequest(studentId, teacherId, "Cross-tenant isolation test."));

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/messages/conversation?studentUserId={studentId}&participantUserId={teacherId}",
            parentId,
            tenantA,
            TestJwt.ParentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<TResponse> SendAsAsync<TResponse>(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        string role,
        object? body = null)
    {
        using var request = TestJwt.Authorized(method, url, userId, tenantId, role);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}
