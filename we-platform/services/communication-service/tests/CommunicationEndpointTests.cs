using System.Net;
using System.Net.Http.Json;
using CommunicationService.Application;
using CommunicationService.Domain;
using CommunicationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WePlatform.Events;

namespace CommunicationService.Tests;

public class CommunicationEndpointTests : IClassFixture<CommunicationWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;
    private readonly CommunicationWebApplicationFactory _factory;
    private readonly FakeDomainEventPublisher _eventPublisher;

    public CommunicationEndpointTests(CommunicationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _factory = factory;
        _eventPublisher = factory.EventPublisher;
    }

    [Fact]
    public async Task Parent_SendsMessageToChildTeacher()
    {
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentId, studentId);
        _accessChecker.AllowTeacher(teacherId, studentId);

        var created = await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentId, teacherId, "How is my child progressing in maths?"));

        Assert.Equal(studentId, created.StudentUserId);
        Assert.Equal(parentId, created.ParentUserId);
        Assert.Equal(teacherId, created.TeacherUserId);
        Assert.Equal(parentId, created.SenderUserId);
        Assert.Equal(MessageSenderRoles.Parent, created.SenderRole);
        Assert.Equal("How is my child progressing in maths?", created.Body);

        var publishedEvent = _eventPublisher.MessageSentEvents.Single(e => e.MessageId == created.Id);
        Assert.Equal(teacherId, publishedEvent.RecipientUserId);
        Assert.Equal(parentId, publishedEvent.SenderUserId);
    }

    [Fact]
    public async Task Teacher_RepliesToParentMessage()
    {
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentId, studentId);
        _accessChecker.AllowTeacher(teacherId, studentId);

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentId, teacherId, "Could we discuss assessment results?"));

        var reply = await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            teacherId,
            TestJwt.TeacherRole,
            new SendMessageRequest(studentId, parentId, "Happy to meet next week to review progress."));

        Assert.Equal(MessageSenderRoles.Teacher, reply.SenderRole);
        Assert.Equal(teacherId, reply.SenderUserId);

        var conversation = await SendAsAsync<ConversationResponse>(
            HttpMethod.Get,
            $"/api/v1/messages/conversation?studentUserId={studentId}&participantUserId={teacherId}",
            parentId,
            TestJwt.ParentRole);

        Assert.Equal(2, conversation.Messages.Count);
        Assert.Equal("Happy to meet next week to review progress.", conversation.Messages[^1].Body);
    }

    [Fact]
    public async Task Messages_AreScopedToStudentContext()
    {
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentOneId = Guid.NewGuid().ToString();
        var studentTwoId = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentId, studentOneId);
        _accessChecker.AllowParent(parentId, studentTwoId);
        _accessChecker.AllowTeacher(teacherId, studentOneId);
        _accessChecker.AllowTeacher(teacherId, studentTwoId);

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentOneId, teacherId, "Question about student one."));

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentTwoId, teacherId, "Question about student two."));

        var studentOneConversation = await SendAsAsync<ConversationResponse>(
            HttpMethod.Get,
            $"/api/v1/messages/conversation?studentUserId={studentOneId}&participantUserId={teacherId}",
            parentId,
            TestJwt.ParentRole);

        Assert.Single(studentOneConversation.Messages);
        Assert.Equal("Question about student one.", studentOneConversation.Messages[0].Body);
    }

    [Fact]
    public async Task Teacher_SeesOnlyMessagesFromParentsInTheirClasses()
    {
        var teacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();
        var parentOneId = Guid.NewGuid().ToString();
        var parentTwoId = Guid.NewGuid().ToString();
        var studentOneId = Guid.NewGuid().ToString();
        var studentTwoId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentOneId);
        _accessChecker.AllowTeacher(otherTeacherId, studentTwoId);
        _accessChecker.AllowParent(parentOneId, studentOneId);
        _accessChecker.AllowParent(parentTwoId, studentTwoId);

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentOneId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentOneId, teacherId, "Message for assigned teacher."));

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentTwoId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentTwoId, otherTeacherId, "Message for another teacher."));

        var inbox = await SendAsAsync<MessageInboxResponse>(
            HttpMethod.Get,
            "/api/v1/messages/inbox",
            teacherId,
            TestJwt.TeacherRole);

        var thread = Assert.Single(inbox.Threads);
        Assert.Equal(studentOneId, thread.StudentUserId);
        Assert.Equal(parentOneId, thread.ParentUserId);
        Assert.Equal("Message for assigned teacher.", thread.LatestMessage.Body);
    }

    [Fact]
    public async Task Parent_SeesOwnMessageThreads()
    {
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentId, studentId);
        _accessChecker.AllowTeacher(teacherId, studentId);

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentId, teacherId, "Follow-up question."));

        var inbox = await SendAsAsync<MessageInboxResponse>(
            HttpMethod.Get,
            "/api/v1/messages/inbox",
            parentId,
            TestJwt.ParentRole);

        var thread = Assert.Single(inbox.Threads);
        Assert.Equal(studentId, thread.StudentUserId);
        Assert.Equal(teacherId, thread.TeacherUserId);
    }

    [Fact]
    public async Task UnrelatedTeacher_CannotReadConversation()
    {
        var assignedTeacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(assignedTeacherId, studentId);
        _accessChecker.AllowParent(parentId, studentId);

        await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentId, assignedTeacherId, "Private parent message."));

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/messages/conversation?studentUserId={studentId}&participantUserId={parentId}",
            otherTeacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnrelatedParent_CannotSendMessage()
    {
        var linkedParentId = Guid.NewGuid().ToString();
        var unrelatedParentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(linkedParentId, studentId);
        _accessChecker.AllowTeacher(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/messages",
            unrelatedParentId,
            TestJwt.ParentRole);
        request.Content = JsonContent.Create(
            new SendMessageRequest(studentId, teacherId, "Should not send."));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MessageHistory_IsPersistedAndAuditable()
    {
        var parentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowParent(parentId, studentId);
        _accessChecker.AllowTeacher(teacherId, studentId);

        var created = await SendAsAsync<MessageResponse>(
            HttpMethod.Post,
            "/api/v1/messages",
            parentId,
            TestJwt.ParentRole,
            new SendMessageRequest(studentId, teacherId, "Please share recent feedback."));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        var persisted = await db.Messages.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == created.Id);

        Assert.NotNull(persisted);
        Assert.Equal(parentId, persisted!.SenderUserId);
        Assert.Equal(MessageSenderRoles.Parent, persisted.SenderRole);
        Assert.True(persisted.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Student_CannotSendMessages()
    {
        var studentId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var parentId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/messages",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(
            new SendMessageRequest(studentId, teacherId, "Student should not message."));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<TResponse> SendAsAsync<TResponse>(
        HttpMethod method,
        string url,
        string userId,
        string role,
        object? body = null)
    {
        using var request = TestJwt.Authorized(method, url, userId, role);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}
