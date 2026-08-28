using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CurriculumService.Application;
using CurriculumService.Domain;
using CurriculumService.Infrastructure.Data;

namespace CurriculumService.Api;

public static class CurriculumEndpoints
{
    public static void MapCurriculumEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/curriculum").RequireAuthorization();

        api.MapGet("/", ListCurricula);
        api.MapPost("/", CreateCurriculum);
        api.MapGet("/{curriculumId:guid}", GetCurriculum);
        api.MapPut("/{curriculumId:guid}", UpdateCurriculum);
        api.MapDelete("/{curriculumId:guid}", DeleteCurriculum);
        api.MapGet("/{curriculumId:guid}/tree", GetTree);

        api.MapGet("/{curriculumId:guid}/subjects", ListSubjects);
        api.MapPost("/{curriculumId:guid}/subjects", CreateSubject);
        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}", GetSubject);
        api.MapPut("/{curriculumId:guid}/subjects/{subjectId:guid}", UpdateSubject);
        api.MapDelete("/{curriculumId:guid}/subjects/{subjectId:guid}", DeleteSubject);

        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units", ListUnits);
        api.MapPost("/{curriculumId:guid}/subjects/{subjectId:guid}/units", CreateUnit);
        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}", GetUnit);
        api.MapPut("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}", UpdateUnit);
        api.MapDelete("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}", DeleteUnit);

        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/topics", ListTopics);
        api.MapPost("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/topics", CreateTopic);
        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/topics/{topicId:guid}", GetTopic);
        api.MapPut("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/topics/{topicId:guid}", UpdateTopic);
        api.MapDelete("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/topics/{topicId:guid}", DeleteTopic);
    }

    private static async Task<IResult> ListCurricula(
        Guid? organisationId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var query = db.Curricula.AsQueryable();
        if (organisationId is { } orgId && orgId != Guid.Empty)
        {
            query = query.Where(item => item.OrganisationId == orgId);
        }

        var items = await query
            .OrderBy(item => item.Name)
            .Select(item => new CurriculumResponse(
                item.Id,
                item.OrganisationId,
                item.Name,
                item.Version,
                item.Status))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateCurriculum(
        CreateCurriculumRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        if (request.OrganisationId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Version))
        {
            return Results.BadRequest();
        }

        var status = string.IsNullOrWhiteSpace(request.Status)
            ? CurriculumStatuses.Draft
            : request.Status.Trim();
        if (!IsValidStatus(status))
        {
            return Results.BadRequest();
        }

        var curriculum = new Curriculum
        {
            Id = Guid.CreateVersion7(),
            OrganisationId = request.OrganisationId,
            Name = request.Name.Trim(),
            Version = request.Version.Trim(),
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Curricula.Add(curriculum);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/curriculum/{curriculum.Id}", ToCurriculum(curriculum));
    }

    private static async Task<IResult> GetCurriculum(
        Guid curriculumId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula.FindAsync(curriculumId);
        return curriculum is null ? Results.NotFound() : Results.Ok(ToCurriculum(curriculum));
    }

    private static async Task<IResult> UpdateCurriculum(
        Guid curriculumId,
        UpdateCurriculumRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula.FindAsync(curriculumId);
        if (curriculum is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Version)
            || !IsValidStatus(request.Status))
        {
            return Results.BadRequest();
        }

        curriculum.Name = request.Name.Trim();
        curriculum.Version = request.Version.Trim();
        curriculum.Status = request.Status.Trim();
        await db.SaveChangesAsync();
        return Results.Ok(ToCurriculum(curriculum));
    }

    private static async Task<IResult> DeleteCurriculum(
        Guid curriculumId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula.FindAsync(curriculumId);
        if (curriculum is null)
        {
            return Results.NotFound();
        }

        db.Curricula.Remove(curriculum);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetTree(
        Guid curriculumId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula
            .Include(item => item.Subjects)
            .ThenInclude(subject => subject.Units)
            .ThenInclude(unit => unit.Topics)
            .FirstOrDefaultAsync(item => item.Id == curriculumId);

        if (curriculum is null)
        {
            return Results.NotFound();
        }

        var subjects = curriculum.Subjects
            .OrderBy(subject => subject.SortOrder)
            .ThenBy(subject => subject.Name)
            .Select(subject => new SubjectTreeResponse(
                subject.Id,
                subject.Name,
                subject.Code,
                subject.SortOrder,
                subject.Units
                    .OrderBy(unit => unit.SortOrder)
                    .ThenBy(unit => unit.Name)
                    .Select(unit => new UnitTreeResponse(
                        unit.Id,
                        unit.Name,
                        unit.SortOrder,
                        unit.Topics
                            .OrderBy(topic => topic.SortOrder)
                            .ThenBy(topic => topic.Name)
                            .Select(topic => ToTopic(topic, subject.Id, curriculum.Id))
                            .ToList()))
                    .ToList()))
            .ToList();

        return Results.Ok(new CurriculumTreeResponse(
            curriculum.Id,
            curriculum.OrganisationId,
            curriculum.Name,
            curriculum.Version,
            curriculum.Status,
            subjects));
    }

    private static async Task<IResult> ListSubjects(
        Guid curriculumId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        if (await db.Curricula.FindAsync(curriculumId) is null)
        {
            return Results.NotFound();
        }

        var items = await db.Subjects
            .Where(subject => subject.CurriculumId == curriculumId)
            .OrderBy(subject => subject.SortOrder)
            .Select(subject => new SubjectResponse(
                subject.Id,
                subject.CurriculumId,
                subject.Name,
                subject.Code,
                subject.SortOrder))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateSubject(
        Guid curriculumId,
        CreateSubjectRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        if (await db.Curricula.FindAsync(curriculumId) is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        if (await db.Subjects.AnyAsync(subject =>
                subject.CurriculumId == curriculumId && subject.Code == request.Code.Trim()))
        {
            return Results.Conflict();
        }

        var subject = new Subject
        {
            Id = Guid.CreateVersion7(),
            CurriculumId = curriculumId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            SortOrder = request.SortOrder
        };

        db.Subjects.Add(subject);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/curriculum/{curriculumId}/subjects/{subject.Id}",
            ToSubject(subject));
    }

    private static async Task<IResult> GetSubject(
        Guid curriculumId,
        Guid subjectId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId);
        return subject is null ? Results.NotFound() : Results.Ok(ToSubject(subject));
    }

    private static async Task<IResult> UpdateSubject(
        Guid curriculumId,
        Guid subjectId,
        UpdateSubjectRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId);
        if (subject is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        if (await db.Subjects.AnyAsync(item =>
                item.CurriculumId == curriculumId && item.Code == request.Code.Trim() && item.Id != subjectId))
        {
            return Results.Conflict();
        }

        subject.Name = request.Name.Trim();
        subject.Code = request.Code.Trim();
        subject.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        return Results.Ok(ToSubject(subject));
    }

    private static async Task<IResult> DeleteSubject(
        Guid curriculumId,
        Guid subjectId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId);
        if (subject is null)
        {
            return Results.NotFound();
        }

        db.Subjects.Remove(subject);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListUnits(
        Guid curriculumId,
        Guid subjectId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId);
        if (subject is null)
        {
            return Results.NotFound();
        }

        var items = await db.Units
            .Where(unit => unit.SubjectId == subject.Id)
            .OrderBy(unit => unit.SortOrder)
            .Select(unit => new UnitResponse(
                unit.Id,
                unit.SubjectId,
                curriculumId,
                unit.Name,
                unit.SortOrder))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateUnit(
        Guid curriculumId,
        Guid subjectId,
        CreateUnitRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId);
        if (subject is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        var unit = new Unit
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subject.Id,
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder
        };

        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unit.Id}",
            ToUnit(unit, curriculumId));
    }

    private static async Task<IResult> GetUnit(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId);
        return unit is null ? Results.NotFound() : Results.Ok(ToUnit(unit, curriculumId));
    }

    private static async Task<IResult> UpdateUnit(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        UpdateUnitRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId);
        if (unit is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        unit.Name = request.Name.Trim();
        unit.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        return Results.Ok(ToUnit(unit, curriculumId));
    }

    private static async Task<IResult> DeleteUnit(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId);
        if (unit is null)
        {
            return Results.NotFound();
        }

        db.Units.Remove(unit);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListTopics(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId);
        if (unit is null)
        {
            return Results.NotFound();
        }

        var items = await db.Topics
            .Where(topic => topic.UnitId == unit.Id)
            .OrderBy(topic => topic.SortOrder)
            .Select(topic => new TopicResponse(
                topic.Id,
                topic.UnitId,
                subjectId,
                curriculumId,
                topic.Name,
                topic.SortOrder))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateTopic(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        CreateTopicRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId);
        if (unit is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        var topic = new Topic
        {
            Id = Guid.CreateVersion7(),
            UnitId = unit.Id,
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder
        };

        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/topics/{topic.Id}",
            ToTopic(topic, subjectId, curriculumId));
    }

    private static async Task<IResult> GetTopic(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid topicId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var topic = await FindTopicAsync(db, curriculumId, subjectId, unitId, topicId);
        return topic is null ? Results.NotFound() : Results.Ok(ToTopic(topic, subjectId, curriculumId));
    }

    private static async Task<IResult> UpdateTopic(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid topicId,
        UpdateTopicRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var topic = await FindTopicAsync(db, curriculumId, subjectId, unitId, topicId);
        if (topic is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        topic.Name = request.Name.Trim();
        topic.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        return Results.Ok(ToTopic(topic, subjectId, curriculumId));
    }

    private static async Task<IResult> DeleteTopic(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid topicId,
        ClaimsPrincipal principal,
        CurriculumDbContext db)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var topic = await FindTopicAsync(db, curriculumId, subjectId, unitId, topicId);
        if (topic is null)
        {
            return Results.NotFound();
        }

        db.Topics.Remove(topic);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<Subject?> FindSubjectAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId) =>
        await db.Subjects.FirstOrDefaultAsync(subject =>
            subject.Id == subjectId && subject.CurriculumId == curriculumId);

    private static async Task<Unit?> FindUnitAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId) =>
        await db.Units.FirstOrDefaultAsync(unit =>
            unit.Id == unitId
            && unit.SubjectId == subjectId
            && unit.Subject.CurriculumId == curriculumId);

    private static async Task<Topic?> FindTopicAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid topicId) =>
        await db.Topics.FirstOrDefaultAsync(topic =>
            topic.Id == topicId
            && topic.UnitId == unitId
            && topic.Unit.SubjectId == subjectId
            && topic.Unit.Subject.CurriculumId == curriculumId);

    private static CurriculumResponse ToCurriculum(Curriculum curriculum) =>
        new(curriculum.Id, curriculum.OrganisationId, curriculum.Name, curriculum.Version, curriculum.Status);

    private static SubjectResponse ToSubject(Subject subject) =>
        new(subject.Id, subject.CurriculumId, subject.Name, subject.Code, subject.SortOrder);

    private static UnitResponse ToUnit(Unit unit, Guid curriculumId) =>
        new(unit.Id, unit.SubjectId, curriculumId, unit.Name, unit.SortOrder);

    private static TopicResponse ToTopic(Topic topic, Guid subjectId, Guid curriculumId) =>
        new(topic.Id, topic.UnitId, subjectId, curriculumId, topic.Name, topic.SortOrder);

    private static bool IsValidStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && CurriculumStatuses.All.Contains(status.Trim());

    private static bool CanManageCurriculum(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Teacher)
        || principal.IsInRole(PlatformRoles.SystemAdministrator);
}
