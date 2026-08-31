using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CurriculumService.Application;
using CurriculumService.Domain;
using CurriculumService.Infrastructure.Data;
using WePlatform.Tenancy;

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

        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives", ListLearningObjectives);
        api.MapPost("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives", CreateLearningObjective);
        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}", GetLearningObjective);
        api.MapPut("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}", UpdateLearningObjective);
        api.MapDelete("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}", DeleteLearningObjective);

        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}/micro-skills", ListMicroSkills);
        api.MapPost("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}/micro-skills", CreateMicroSkill);
        api.MapGet("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}/micro-skills/{microSkillId:guid}", GetMicroSkill);
        api.MapPut("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}/micro-skills/{microSkillId:guid}", UpdateMicroSkill);
        api.MapDelete("/{curriculumId:guid}/subjects/{subjectId:guid}/units/{unitId:guid}/learning-objectives/{learningObjectiveId:guid}/micro-skills/{microSkillId:guid}", DeleteMicroSkill);
    }

    private static async Task<IResult> ListCurricula(
        Guid? organisationId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
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

        if (!tenantContext.HasTenant || request.OrganisationId != tenantContext.TenantId)
        {
            return Results.Forbid();
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
            TenantId = request.OrganisationId,
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == curriculumId);
        if (curriculum is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, curriculum);
        return tenantAccess is not null ? tenantAccess : Results.Ok(ToCurriculum(curriculum));
    }

    private static async Task<IResult> UpdateCurriculum(
        Guid curriculumId,
        UpdateCurriculumRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == curriculumId);
        if (curriculum is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, curriculum);
        if (tenantAccess is not null)
        {
            return tenantAccess;
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == curriculumId);
        if (curriculum is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, curriculum);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        db.Curricula.Remove(curriculum);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetTree(
        Guid curriculumId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var curriculum = await db.Curricula
            .IgnoreQueryFilters()
            .Include(item => item.Subjects)
            .ThenInclude(subject => subject.Units)
            .ThenInclude(unit => unit.Topics)
            .Include(item => item.Subjects)
            .ThenInclude(subject => subject.Units)
            .ThenInclude(unit => unit.LearningObjectives)
            .ThenInclude(objective => objective.MicroSkills)
            .FirstOrDefaultAsync(item => item.Id == curriculumId);

        if (curriculum is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, curriculum);
        if (tenantAccess is not null)
        {
            return tenantAccess;
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
                            .ToList(),
                        unit.LearningObjectives
                            .OrderBy(objective => objective.SortOrder)
                            .ThenBy(objective => objective.Title)
                            .Select(objective => new LearningObjectiveTreeResponse(
                                objective.Id,
                                objective.Title,
                                objective.SortOrder,
                                objective.MicroSkills
                                    .OrderBy(skill => skill.SortOrder)
                                    .ThenBy(skill => skill.Name)
                                    .Select(skill => new MicroSkillTreeResponse(
                                        skill.Id,
                                        skill.Name,
                                        skill.SortOrder))
                                    .ToList()))
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        if (await FindCurriculumAsync(db, curriculumId, tenantContext) is null)
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        if (await FindCurriculumAsync(db, curriculumId, tenantContext) is null)
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId, tenantContext);
        return subject is null ? Results.NotFound() : Results.Ok(ToSubject(subject));
    }

    private static async Task<IResult> UpdateSubject(
        Guid curriculumId,
        Guid subjectId,
        UpdateSubjectRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var subject = await FindSubjectAsync(db, curriculumId, subjectId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId, tenantContext);
        return unit is null ? Results.NotFound() : Results.Ok(ToUnit(unit, curriculumId));
    }

    private static async Task<IResult> UpdateUnit(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        UpdateUnitRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var topic = await FindTopicAsync(db, curriculumId, subjectId, unitId, topicId, tenantContext);
        return topic is null ? Results.NotFound() : Results.Ok(ToTopic(topic, subjectId, curriculumId));
    }

    private static async Task<IResult> UpdateTopic(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid topicId,
        UpdateTopicRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var topic = await FindTopicAsync(db, curriculumId, subjectId, unitId, topicId, tenantContext);
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
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var topic = await FindTopicAsync(db, curriculumId, subjectId, unitId, topicId, tenantContext);
        if (topic is null)
        {
            return Results.NotFound();
        }

        db.Topics.Remove(topic);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListLearningObjectives(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId, tenantContext);
        if (unit is null)
        {
            return Results.NotFound();
        }

        var items = await db.LearningObjectives
            .Where(objective => objective.UnitId == unit.Id)
            .OrderBy(objective => objective.SortOrder)
            .Select(objective => new LearningObjectiveResponse(
                objective.Id,
                objective.UnitId,
                subjectId,
                curriculumId,
                objective.Title,
                objective.SortOrder))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateLearningObjective(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        CreateLearningObjectiveRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var unit = await FindUnitAsync(db, curriculumId, subjectId, unitId, tenantContext);
        if (unit is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest();
        }

        var objective = new LearningObjective
        {
            Id = Guid.CreateVersion7(),
            UnitId = unit.Id,
            Title = request.Title.Trim(),
            SortOrder = request.SortOrder
        };

        db.LearningObjectives.Add(objective);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/learning-objectives/{objective.Id}",
            ToLearningObjective(objective, subjectId, curriculumId));
    }

    private static async Task<IResult> GetLearningObjective(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var objective = await FindLearningObjectiveAsync(db, curriculumId, subjectId, unitId, learningObjectiveId, tenantContext);
        return objective is null
            ? Results.NotFound()
            : Results.Ok(ToLearningObjective(objective, subjectId, curriculumId));
    }

    private static async Task<IResult> UpdateLearningObjective(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        UpdateLearningObjectiveRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var objective = await FindLearningObjectiveAsync(db, curriculumId, subjectId, unitId, learningObjectiveId, tenantContext);
        if (objective is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest();
        }

        objective.Title = request.Title.Trim();
        objective.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        return Results.Ok(ToLearningObjective(objective, subjectId, curriculumId));
    }

    private static async Task<IResult> DeleteLearningObjective(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var objective = await FindLearningObjectiveAsync(db, curriculumId, subjectId, unitId, learningObjectiveId, tenantContext);
        if (objective is null)
        {
            return Results.NotFound();
        }

        db.LearningObjectives.Remove(objective);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListMicroSkills(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var objective = await FindLearningObjectiveAsync(db, curriculumId, subjectId, unitId, learningObjectiveId, tenantContext);
        if (objective is null)
        {
            return Results.NotFound();
        }

        var items = await db.MicroSkills
            .Where(skill => skill.LearningObjectiveId == objective.Id)
            .OrderBy(skill => skill.SortOrder)
            .Select(skill => new MicroSkillResponse(
                skill.Id,
                skill.LearningObjectiveId,
                unitId,
                subjectId,
                curriculumId,
                skill.Name,
                skill.SortOrder))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateMicroSkill(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        CreateMicroSkillRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var objective = await FindLearningObjectiveAsync(db, curriculumId, subjectId, unitId, learningObjectiveId, tenantContext);
        if (objective is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        var microSkill = new MicroSkill
        {
            Id = Guid.CreateVersion7(),
            LearningObjectiveId = objective.Id,
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder
        };

        db.MicroSkills.Add(microSkill);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/learning-objectives/{learningObjectiveId}/micro-skills/{microSkill.Id}",
            ToMicroSkill(microSkill, unitId, subjectId, curriculumId));
    }

    private static async Task<IResult> GetMicroSkill(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        Guid microSkillId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var microSkill = await FindMicroSkillAsync(
            db,
            curriculumId,
            subjectId,
            unitId,
            learningObjectiveId,
            microSkillId,
            tenantContext);
        return microSkill is null
            ? Results.NotFound()
            : Results.Ok(ToMicroSkill(microSkill, unitId, subjectId, curriculumId));
    }

    private static async Task<IResult> UpdateMicroSkill(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        Guid microSkillId,
        UpdateMicroSkillRequest request,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var microSkill = await FindMicroSkillAsync(
            db,
            curriculumId,
            subjectId,
            unitId,
            learningObjectiveId,
            microSkillId,
            tenantContext);
        if (microSkill is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        microSkill.Name = request.Name.Trim();
        microSkill.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        return Results.Ok(ToMicroSkill(microSkill, unitId, subjectId, curriculumId));
    }

    private static async Task<IResult> DeleteMicroSkill(
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        Guid microSkillId,
        ClaimsPrincipal principal,
        CurriculumDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageCurriculum())
        {
            return Results.Forbid();
        }

        var microSkill = await FindMicroSkillAsync(
            db,
            curriculumId,
            subjectId,
            unitId,
            learningObjectiveId,
            microSkillId,
            tenantContext);
        if (microSkill is null)
        {
            return Results.NotFound();
        }

        db.MicroSkills.Remove(microSkill);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<Subject?> FindSubjectAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId,
        ITenantContext tenantContext)
    {
        var subject = await db.Subjects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.CurriculumId == curriculumId);
        if (subject is null)
        {
            return null;
        }

        return TenantAccess.ValidateEntityAccess(tenantContext, subject) is null ? subject : null;
    }

    private static async Task<Unit?> FindUnitAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        ITenantContext tenantContext)
    {
        var unit = await db.Units
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u =>
                u.Id == unitId
                && u.SubjectId == subjectId
                && u.Subject.CurriculumId == curriculumId);
        if (unit is null)
        {
            return null;
        }

        return TenantAccess.ValidateEntityAccess(tenantContext, unit) is null ? unit : null;
    }

    private static async Task<Topic?> FindTopicAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid topicId,
        ITenantContext tenantContext)
    {
        var topic = await db.Topics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t =>
                t.Id == topicId
                && t.UnitId == unitId
                && t.Unit.SubjectId == subjectId
                && t.Unit.Subject.CurriculumId == curriculumId);
        if (topic is null)
        {
            return null;
        }

        return TenantAccess.ValidateEntityAccess(tenantContext, topic) is null ? topic : null;
    }

    private static async Task<LearningObjective?> FindLearningObjectiveAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        ITenantContext tenantContext)
    {
        var objective = await db.LearningObjectives
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o =>
                o.Id == learningObjectiveId
                && o.UnitId == unitId
                && o.Unit.SubjectId == subjectId
                && o.Unit.Subject.CurriculumId == curriculumId);
        if (objective is null)
        {
            return null;
        }

        return TenantAccess.ValidateEntityAccess(tenantContext, objective) is null ? objective : null;
    }

    private static async Task<MicroSkill?> FindMicroSkillAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        Guid microSkillId,
        ITenantContext tenantContext)
    {
        var skill = await db.MicroSkills
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s =>
                s.Id == microSkillId
                && s.LearningObjectiveId == learningObjectiveId
                && s.LearningObjective.UnitId == unitId
                && s.LearningObjective.Unit.SubjectId == subjectId
                && s.LearningObjective.Unit.Subject.CurriculumId == curriculumId);
        if (skill is null)
        {
            return null;
        }

        return TenantAccess.ValidateEntityAccess(tenantContext, skill) is null ? skill : null;
    }

    private static async Task<Curriculum?> FindCurriculumAsync(
        CurriculumDbContext db,
        Guid curriculumId,
        ITenantContext tenantContext)
    {
        var curriculum = await db.Curricula.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == curriculumId);
        if (curriculum is null)
        {
            return null;
        }

        return TenantAccess.ValidateEntityAccess(tenantContext, curriculum) is null ? curriculum : null;
    }

    private static CurriculumResponse ToCurriculum(Curriculum curriculum) =>
        new(curriculum.Id, curriculum.OrganisationId, curriculum.Name, curriculum.Version, curriculum.Status);

    private static SubjectResponse ToSubject(Subject subject) =>
        new(subject.Id, subject.CurriculumId, subject.Name, subject.Code, subject.SortOrder);

    private static UnitResponse ToUnit(Unit unit, Guid curriculumId) =>
        new(unit.Id, unit.SubjectId, curriculumId, unit.Name, unit.SortOrder);

    private static TopicResponse ToTopic(Topic topic, Guid subjectId, Guid curriculumId) =>
        new(topic.Id, topic.UnitId, subjectId, curriculumId, topic.Name, topic.SortOrder);

    private static LearningObjectiveResponse ToLearningObjective(
        LearningObjective objective,
        Guid subjectId,
        Guid curriculumId) =>
        new(objective.Id, objective.UnitId, subjectId, curriculumId, objective.Title, objective.SortOrder);

    private static MicroSkillResponse ToMicroSkill(
        MicroSkill microSkill,
        Guid unitId,
        Guid subjectId,
        Guid curriculumId) =>
        new(
            microSkill.Id,
            microSkill.LearningObjectiveId,
            unitId,
            subjectId,
            curriculumId,
            microSkill.Name,
            microSkill.SortOrder);

    private static bool IsValidStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && CurriculumStatuses.All.Contains(status.Trim());

    private static bool CanManageCurriculum(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Teacher)
        || principal.IsInRole(PlatformRoles.SystemAdministrator);
}
