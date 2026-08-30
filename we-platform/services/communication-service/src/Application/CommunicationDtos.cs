namespace CommunicationService.Application;

public sealed record SendMessageRequest(
    string StudentUserId,
    string RecipientUserId,
    string Body);

public sealed record MessageResponse(
    Guid Id,
    string StudentUserId,
    string ParentUserId,
    string TeacherUserId,
    string SenderUserId,
    string SenderRole,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record ConversationResponse(
    string StudentUserId,
    string ParentUserId,
    string TeacherUserId,
    IReadOnlyList<MessageResponse> Messages);

public sealed record MessageInboxResponse(
    IReadOnlyList<MessageInboxThreadResponse> Threads);

public sealed record MessageInboxThreadResponse(
    string StudentUserId,
    string ParentUserId,
    string TeacherUserId,
    MessageResponse LatestMessage,
    int MessageCount);
