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
    public async Task CrossSchool_Messages_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: communication-service:messages
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var parentA = Guid.NewGuid().ToString();
        var parentB = Guid.NewGuid().ToString();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentA, studentA);
        _accessChecker.AllowTeacher(teacherA, studentA);
        _accessChecker.AllowParent(parentB, studentB);
        _accessChecker.AllowTeacher(teacherB, studentB);

        const string bodyA = "School A only thread.";
        const string bodyB = "School B only thread.";

        await SendMessageAsync(parentA, schoolA, studentA, teacherA, bodyA);
        await SendMessageAsync(parentB, schoolB, studentB, teacherB, bodyB);

        await AssertInboxDoesNotContainAsync(parentB, schoolA, bodyB);
        await AssertInboxDoesNotContainAsync(parentA, schoolB, bodyA);

        await AssertConversationDoesNotContainAsync(
            parentB,
            schoolA,
            studentA,
            teacherA,
            bodyA);
        await AssertConversationDoesNotContainAsync(
            parentA,
            schoolB,
            studentB,
            teacherB,
            bodyB);

        using var sendIntoA = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/messages",
            parentB,
            schoolB,
            TestJwt.ParentRole);
        sendIntoA.Content = JsonContent.Create(new SendMessageRequest(studentA, teacherA, "Cross-school send."));
        var sendResponse = await _client.SendAsync(sendIntoA);
        Assert.True(
            sendResponse.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.BadRequest,
            $"Cross-school send returned {sendResponse.StatusCode}");
    }

    private async Task SendMessageAsync(
        string parentId,
        Guid tenantId,
        string studentId,
        string teacherId,
        string body)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            tenantId,
            TestJwt.ParentRole);
        request.Content = JsonContent.Create(new SendMessageRequest(studentId, teacherId, body));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task AssertInboxDoesNotContainAsync(string userId, Guid tenantId, string forbiddenBody)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/messages/inbox",
            userId,
            tenantId,
            TestJwt.ParentRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var inbox = await response.Content.ReadFromJsonAsync<MessageInboxResponse>();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(forbiddenBody, payload, StringComparison.Ordinal);
        Assert.All(inbox!.Threads, thread => Assert.DoesNotContain(forbiddenBody, thread.LatestMessage.Body));
    }

    private async Task AssertConversationDoesNotContainAsync(
        string parentId,
        Guid tenantId,
        string studentId,
        string teacherId,
        string forbiddenBody)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/messages/conversation?studentUserId={studentId}&participantUserId={teacherId}",
            parentId,
            tenantId,
            TestJwt.ParentRole);
        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.OK,
            $"conversation returned {response.StatusCode}");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
            Assert.All(conversation!.Messages, m => Assert.DoesNotContain(forbiddenBody, m.Body));
        }
    }
}
