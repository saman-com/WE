using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InterventionService.Application;
using InterventionService.Domain;
using InterventionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace InterventionService.Api;

public static class InterventionEndpoints
{
    public static void MapInterventionEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/interventions").RequireAuthorization();

        api.MapPost("/", CreateIntervention);
        api.MapGet("/", ListInterventions);
        api.MapGet("/parent-summary", ListParentInterventionSummaries);
        api.MapGet("/{interventionId:guid}", GetIntervention);
        api.MapPatch("/{interventionId:guid}", PatchIntervention);

        var orgApi = app.MapGroup("/api/v1/organisations").RequireAuthorization();
        orgApi.MapGet("/{organisationId:guid}/interventions", ListOrganisationInterventions);
    }

    private static async Task<IResult> CreateIntervention(
        CreateInterventionRequest request,
        ClaimsPrincipal principal,
        InterventionDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (!principal.IsTeacher() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        if (request.OrganisationId != tenantContext.TenantId)
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.StudentUserId)
            || string.IsNullOrWhiteSpace(request.PlannedActions))
        {
            return Results.BadRequest();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        if (principal.IsTeacher())
        {
            var canCreate = await accessChecker.TeacherCanViewStudentAsync(
                principal.UserId(),
                request.StudentUserId,
                bearerToken);
            if (!canCreate)
            {
                return Results.Forbid();
            }
        }

        var now = DateTimeOffset.UtcNow;
        var intervention = new Intervention
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantContext.TenantId!.Value,
            OrganisationId = request.OrganisationId,
            StudentUserId = request.StudentUserId.Trim(),
            LearningGapId = request.LearningGapId,
            AssignedTeacherUserId = principal.UserId(),
            PlannedActions = request.PlannedActions.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? string.Empty : request.Notes.Trim(),
            Status = InterventionStatuses.Planned,
            PlannedStartAt = request.PlannedStartAt,
            PlannedEndAt = request.PlannedEndAt,
            ReviewAt = request.ReviewAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Interventions.Add(intervention);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/interventions/{intervention.Id}", ToResponse(intervention));
    }

    private static async Task<IResult> ListInterventions(
        string? studentUserId,
        ClaimsPrincipal principal,
        InterventionDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(studentUserId))
        {
            return Results.BadRequest();
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var access = await EvaluateViewAccessAsync(
            principal,
            studentUserId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var tenantId = tenantContext.TenantId!.Value;
        var all = await db.Interventions
            .IgnoreQueryFilters()
            .Where(i => i.StudentUserId == studentUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        if (all.Count > 0 && all.All(i => i.TenantId != tenantId))
        {
            return Results.Forbid();
        }

        var interventions = all.Where(i => i.TenantId == tenantId).ToList();

        return Results.Ok(new StudentInterventionsResponse(
            studentUserId,
            interventions.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> ListParentInterventionSummaries(
        string? studentUserId,
        ClaimsPrincipal principal,
        InterventionDbContext db,
        IOrganisationAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(studentUserId))
        {
            return Results.BadRequest();
        }

        if (!principal.IsParent())
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var allowed = await accessChecker.ParentCanViewStudentAsync(
            principal.UserId(),
            studentUserId,
            bearerToken);
        if (!allowed)
        {
            return Results.Forbid();
        }

        var interventions = await db.Interventions
            .Where(i => i.StudentUserId == studentUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Results.Ok(new ParentInterventionsResponse(
            studentUserId,
            interventions.Select(ToParentSummary).ToList()));
    }

    private static async Task<IResult> ListOrganisationInterventions(
        Guid organisationId,
        string? status,
        ClaimsPrincipal principal,
        InterventionDbContext db,
        IOrganisationAccessChecker accessChecker,
        HttpContext httpContext)
    {
        if (!principal.IsAdmin() && !principal.IsSchoolLeader())
        {
            return Results.Forbid();
        }

        if (!principal.IsAdmin())
        {
            var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
            if (bearerToken is null)
            {
                return Results.Forbid();
            }

            var allowed = await accessChecker.SchoolLeaderCanViewOrganisationAsync(
                principal.UserId(),
                organisationId,
                bearerToken);
            if (!allowed)
            {
                return Results.Forbid();
            }
        }

        var query = db.Interventions.Where(i => i.OrganisationId == organisationId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusFilter = status.Trim();
            if (!InterventionStatuses.IsValid(statusFilter))
            {
                return Results.BadRequest();
            }

            query = query.Where(i => i.Status == statusFilter);
        }
        else
        {
            query = query.Where(i =>
                i.Status == InterventionStatuses.Planned || i.Status == InterventionStatuses.Active);
        }

        var interventions = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return Results.Ok(new OrganisationInterventionsResponse(
            organisationId,
            interventions.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> GetIntervention(
        Guid interventionId,
        ClaimsPrincipal principal,
        InterventionDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        var intervention = await db.Interventions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == interventionId);
        if (intervention is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, intervention);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        var access = await EvaluateViewAccessAsync(
            principal,
            intervention.StudentUserId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        return Results.Ok(ToResponse(intervention));
    }

    private static async Task<IResult> PatchIntervention(
        Guid interventionId,
        PatchInterventionRequest request,
        ClaimsPrincipal principal,
        InterventionDbContext db,
        ITenantContext tenantContext)
    {
        var intervention = await db.Interventions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == interventionId);
        if (intervention is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, intervention);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        var modifyAccess = await EvaluateModifyAccessAsync(principal, intervention);
        if (modifyAccess is not null)
        {
            return modifyAccess;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var nextStatus = request.Status.Trim();
            if (!InterventionStatuses.IsValid(nextStatus))
            {
                return Results.BadRequest();
            }

            if (!InterventionStatusTransitions.CanTransition(intervention.Status, nextStatus))
            {
                return Results.UnprocessableEntity(new
                {
                    title = "Invalid status transition.",
                    currentStatus = intervention.Status,
                    requestedStatus = nextStatus
                });
            }

            intervention.Status = nextStatus;
        }

        if (request.Notes is not null)
        {
            intervention.Notes = request.Notes.Trim();
        }

        if (request.Outcome is not null)
        {
            intervention.Outcome = string.IsNullOrWhiteSpace(request.Outcome) ? null : request.Outcome.Trim();
        }

        if (request.PlannedActions is not null)
        {
            intervention.PlannedActions = request.PlannedActions.Trim();
        }

        if (request.PlannedStartAt.HasValue)
        {
            intervention.PlannedStartAt = request.PlannedStartAt;
        }

        if (request.PlannedEndAt.HasValue)
        {
            intervention.PlannedEndAt = request.PlannedEndAt;
        }

        if (request.ReviewAt.HasValue)
        {
            intervention.ReviewAt = request.ReviewAt;
        }

        intervention.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(intervention));
    }

    private static async Task<IResult?> EvaluateViewAccessAsync(
        ClaimsPrincipal principal,
        string studentUserId,
        IOrganisationAccessChecker accessChecker,
        string authorizationHeader)
    {
        if (principal.IsAdmin())
        {
            return null;
        }

        if (principal.IsStudent() && principal.UserId() == studentUserId)
        {
            return null;
        }

        var bearerToken = ExtractBearerToken(authorizationHeader);
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        if (principal.IsTeacher())
        {
            var allowed = await accessChecker.TeacherCanViewStudentAsync(
                principal.UserId(),
                studentUserId,
                bearerToken);
            return allowed ? null : Results.Forbid();
        }

        if (principal.IsSchoolLeader())
        {
            var allowed = await accessChecker.SchoolLeaderCanViewStudentAsync(
                principal.UserId(),
                studentUserId,
                bearerToken);
            return allowed ? null : Results.Forbid();
        }

        return Results.Forbid();
    }

    private static Task<IResult?> EvaluateModifyAccessAsync(
        ClaimsPrincipal principal,
        Intervention intervention)
    {
        if (principal.IsAdmin())
        {
            return Task.FromResult<IResult?>(null);
        }

        if (principal.IsTeacher() && principal.UserId() == intervention.AssignedTeacherUserId)
        {
            return Task.FromResult<IResult?>(null);
        }

        return Task.FromResult<IResult?>(Results.Forbid());
    }

    private static InterventionResponse ToResponse(Intervention intervention) =>
        new(
            intervention.Id,
            intervention.OrganisationId,
            intervention.StudentUserId,
            intervention.LearningGapId,
            intervention.AssignedTeacherUserId,
            intervention.PlannedActions,
            intervention.Notes,
            intervention.Outcome,
            intervention.Status,
            intervention.PlannedStartAt,
            intervention.PlannedEndAt,
            intervention.ReviewAt,
            intervention.CreatedAt,
            intervention.UpdatedAt);

    private static ParentInterventionSummaryResponse ToParentSummary(Intervention intervention) =>
        new(
            intervention.Id,
            intervention.PlannedActions,
            intervention.Status,
            intervention.PlannedStartAt,
            intervention.PlannedEndAt);

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

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SystemAdministrator);

    private static bool IsTeacher(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Teacher);

    private static bool IsSchoolLeader(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SchoolLeader);

    private static bool IsStudent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Student);

    private static bool IsParent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Parent);
}
