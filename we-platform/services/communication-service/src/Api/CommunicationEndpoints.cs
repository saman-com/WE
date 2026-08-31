using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CommunicationService.Application;
using CommunicationService.Domain;
using CommunicationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Events;
using WePlatform.Tenancy;

namespace CommunicationService.Api;

public static class CommunicationEndpoints
{
    public static void MapCommunicationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/messages").RequireAuthorization();

        api.MapPost("/", SendMessage);
        api.MapGet("/conversation", GetConversation);
        api.MapGet("/inbox", GetInbox);
    }

    private static async Task<IResult> SendMessage(
        SendMessageRequest request,
        ClaimsPrincipal principal,
        CommunicationDbContext db,
        IOrganisationAccessChecker accessChecker,
        IDomainEventPublisher eventPublisher,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.StudentUserId)
            || string.IsNullOrWhiteSpace(request.RecipientUserId)
            || string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest();
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var studentUserId = request.StudentUserId.Trim();
        var recipientUserId = request.RecipientUserId.Trim();
        var body = request.Body.Trim();
        var senderUserId = principal.UserId();

        string parentUserId;
        string teacherUserId;
        string senderRole;

        if (principal.IsParent())
        {
            teacherUserId = recipientUserId;
            parentUserId = senderUserId;
            senderRole = MessageSenderRoles.Parent;

            if (!await accessChecker.ParentCanViewStudentAsync(parentUserId, studentUserId, bearerToken))
            {
                return Results.Forbid();
            }

            if (!await accessChecker.TeacherCanViewStudentAsync(teacherUserId, studentUserId, bearerToken))
            {
                return Results.Forbid();
            }
        }
        else if (principal.IsTeacher())
        {
            parentUserId = recipientUserId;
            teacherUserId = senderUserId;
            senderRole = MessageSenderRoles.Teacher;

            if (!await accessChecker.TeacherCanViewStudentAsync(teacherUserId, studentUserId, bearerToken))
            {
                return Results.Forbid();
            }

            if (!await accessChecker.ParentCanViewStudentAsync(parentUserId, studentUserId, bearerToken))
            {
                return Results.Forbid();
            }
        }
        else
        {
            return Results.Forbid();
        }

        var message = new ParentTeacherMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantContext.TenantId!.Value,
            StudentUserId = studentUserId,
            ParentUserId = parentUserId,
            TeacherUserId = teacherUserId,
            SenderUserId = senderUserId,
            SenderRole = senderRole,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var eventId = Guid.CreateVersion7();
        var preview = body.Length <= 120 ? body : $"{body[..117]}...";
        await eventPublisher.PublishMessageSentAsync(new MessageSent(
            eventId,
            eventId,
            message.CreatedAt,
            MessageSent.CurrentVersion,
            message.Id,
            studentUserId,
            senderUserId,
            recipientUserId,
            preview));

        return Results.Created($"/api/v1/messages/{message.Id}", ToResponse(message));
    }

    private static async Task<IResult> GetConversation(
        string? studentUserId,
        string? participantUserId,
        ClaimsPrincipal principal,
        CommunicationDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(studentUserId) || string.IsNullOrWhiteSpace(participantUserId))
        {
            return Results.BadRequest();
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var studentId = studentUserId.Trim();
        var participantId = participantUserId.Trim();
        var userId = principal.UserId();

        string parentUserId;
        string teacherUserId;

        if (principal.IsParent())
        {
            parentUserId = userId;
            teacherUserId = participantId;

            if (!await accessChecker.ParentCanViewStudentAsync(parentUserId, studentId, bearerToken))
            {
                return Results.Forbid();
            }

            if (!await accessChecker.TeacherCanViewStudentAsync(teacherUserId, studentId, bearerToken))
            {
                return Results.Forbid();
            }
        }
        else if (principal.IsTeacher())
        {
            teacherUserId = userId;
            parentUserId = participantId;

            if (!await accessChecker.TeacherCanViewStudentAsync(teacherUserId, studentId, bearerToken))
            {
                return Results.Forbid();
            }

            if (!await accessChecker.ParentCanViewStudentAsync(parentUserId, studentId, bearerToken))
            {
                return Results.Forbid();
            }
        }
        else
        {
            return Results.Forbid();
        }

        var messages = await db.Messages
            .IgnoreQueryFilters()
            .Where(m =>
                m.StudentUserId == studentId
                && m.ParentUserId == parentUserId
                && m.TeacherUserId == teacherUserId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        if (messages.Count > 0)
        {
            var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, messages[0]);
            if (tenantAccess is not null)
            {
                return tenantAccess;
            }
        }

        messages = messages
            .Where(m => m.TenantId == tenantContext.TenantId)
            .ToList();

        return Results.Ok(new ConversationResponse(
            studentId,
            parentUserId,
            teacherUserId,
            messages.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> GetInbox(
        ClaimsPrincipal principal,
        CommunicationDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (!principal.IsTeacher() && !principal.IsParent())
        {
            return Results.Forbid();
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var userId = principal.UserId();
        var messages = principal.IsTeacher()
            ? await db.Messages
                .Where(m => m.TeacherUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync()
            : await db.Messages
                .Where(m => m.ParentUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

        var threads = new List<MessageInboxThreadResponse>();
        foreach (var group in messages.GroupBy(m => new { m.StudentUserId, m.ParentUserId, m.TeacherUserId }))
        {
            if (principal.IsTeacher())
            {
                if (!await accessChecker.TeacherCanViewStudentAsync(userId, group.Key.StudentUserId, bearerToken))
                {
                    continue;
                }
            }
            else if (!await accessChecker.ParentCanViewStudentAsync(userId, group.Key.StudentUserId, bearerToken))
            {
                continue;
            }

            var ordered = group.OrderByDescending(m => m.CreatedAt).ToList();
            threads.Add(new MessageInboxThreadResponse(
                group.Key.StudentUserId,
                group.Key.ParentUserId,
                group.Key.TeacherUserId,
                ToResponse(ordered[0]),
                ordered.Count));
        }

        return Results.Ok(new MessageInboxResponse(threads.OrderByDescending(t => t.LatestMessage.CreatedAt).ToList()));
    }

    private static MessageResponse ToResponse(ParentTeacherMessage message) =>
        new(
            message.Id,
            message.StudentUserId,
            message.ParentUserId,
            message.TeacherUserId,
            message.SenderUserId,
            message.SenderRole,
            message.Body,
            message.CreatedAt);

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsTeacher(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Teacher);

    private static bool IsParent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Parent);

    private static string? ExtractBearerToken(string authorizationHeader)
    {
        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorizationHeader[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
