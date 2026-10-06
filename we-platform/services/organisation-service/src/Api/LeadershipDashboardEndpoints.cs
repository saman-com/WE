using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganisationService.Application;
using OrganisationService.Domain;
using OrganisationService.Infrastructure.Data;

namespace OrganisationService.Api;

public static class LeadershipDashboardEndpoints
{
    public static void MapLeadershipDashboardEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/organisations").RequireAuthorization();

        api.MapPost("/{organisationId:guid}/leaders", AssignSchoolLeader);
        api.MapDelete("/{organisationId:guid}/leaders/{userId}", UnassignSchoolLeader);
        api.MapGet("/{organisationId:guid}/leadership/dashboard", GetSchoolLeadershipDashboard);
        api.MapGet("/{organisationId:guid}/year-levels/{yearLevelId:guid}/leadership/dashboard", GetYearLevelLeadershipDashboard);
        api.MapGet("/{organisationId:guid}/classes/{classId:guid}/leadership/summary", GetClassLeadershipSummary);
        api.MapGet("/{organisationId:guid}/leadership/interventions", GetLeadershipInterventionMonitoring);
    }

    private static async Task<IResult> AssignSchoolLeader(
        Guid organisationId,
        AssignSchoolLeaderRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (await db.Organisations.FindAsync(organisationId) is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.BadRequest();
        }

        if (await db.OrganisationLeaders.AnyAsync(
                leader => leader.OrganisationId == organisationId && leader.LeaderUserId == request.UserId))
        {
            return Results.Conflict();
        }

        db.OrganisationLeaders.Add(new OrganisationLeader
        {
            OrganisationId = organisationId,
            LeaderUserId = request.UserId,
            AssignedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/v1/organisations/{organisationId}/leaders/{request.UserId}",
            new SchoolLeaderResponse(request.UserId));
    }

    private static async Task<IResult> UnassignSchoolLeader(
        Guid organisationId,
        string userId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var assignment = await db.OrganisationLeaders.FirstOrDefaultAsync(
            leader => leader.OrganisationId == organisationId && leader.LeaderUserId == userId);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        db.OrganisationLeaders.Remove(assignment);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetSchoolLeadershipDashboard(
        Guid organisationId,
        ClaimsPrincipal principal,
        OrganisationDbContext db,
        IAssessmentDashboardClient assessmentClient,
        IInterventionDashboardClient interventionClient,
        IEiInsightsClient eiClient,
        HttpContext httpContext)
    {
        var access = await EvaluateLeadershipAccessAsync(principal, organisationId, db);
        if (access is not null)
        {
            return access;
        }

        var organisation = await db.Organisations.FindAsync(organisationId);
        if (organisation is null)
        {
            return Results.NotFound();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var classes = await LoadClassesWithYearLevelsAsync(db, organisationId);
        var aggregations = await AggregateClassesAsync(
            classes,
            assessmentClient,
            interventionClient,
            eiClient,
            bearerToken);

        return Results.Ok(new LeadershipDashboardResponse(
            organisationId,
            organisation.Name,
            LeadershipDashboardAggregator.RollUpKpis(aggregations),
            LeadershipDashboardAggregator.RollUpYearLevels(aggregations),
            LeadershipDashboardAggregator.ToClassComparisons(aggregations)));
    }

    private static async Task<IResult> GetYearLevelLeadershipDashboard(
        Guid organisationId,
        Guid yearLevelId,
        ClaimsPrincipal principal,
        OrganisationDbContext db,
        IAssessmentDashboardClient assessmentClient,
        IInterventionDashboardClient interventionClient,
        IEiInsightsClient eiClient,
        HttpContext httpContext)
    {
        var access = await EvaluateLeadershipAccessAsync(principal, organisationId, db);
        if (access is not null)
        {
            return access;
        }

        var yearLevel = await db.YearLevels.FirstOrDefaultAsync(
            level => level.Id == yearLevelId && level.OrganisationId == organisationId);
        if (yearLevel is null)
        {
            return Results.NotFound();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var classes = await LoadClassesWithYearLevelsAsync(db, organisationId, yearLevelId);
        var aggregations = await AggregateClassesAsync(
            classes,
            assessmentClient,
            interventionClient,
            eiClient,
            bearerToken);

        return Results.Ok(new YearLevelLeadershipDashboardResponse(
            organisationId,
            yearLevelId,
            yearLevel.Name,
            LeadershipDashboardAggregator.RollUpKpis(aggregations),
            LeadershipDashboardAggregator.ToClassComparisons(aggregations)));
    }

    private static async Task<IResult> GetClassLeadershipSummary(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        OrganisationDbContext db,
        IAssessmentDashboardClient assessmentClient,
        IInterventionDashboardClient interventionClient,
        IEiInsightsClient eiClient,
        HttpContext httpContext)
    {
        var access = await EvaluateLeadershipAccessAsync(principal, organisationId, db);
        if (access is not null)
        {
            return access;
        }

        var schoolClass = await db.Classes
            .Include(c => c.Teachers)
            .Include(c => c.Enrollments)
            .Include(c => c.YearLevel)
            .FirstOrDefaultAsync(c => c.Id == classId && c.OrganisationId == organisationId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var assessments = await assessmentClient.GetRecentClassAssessmentSummariesAsync(
            organisationId,
            classId,
            bearerToken);
        var eiInsights = await eiClient.GetClassInsightsAsync(organisationId, classId, bearerToken);
        var activeInterventions = await CountActiveInterventionsAsync(
            organisationId,
            schoolClass.Enrollments.Select(e => e.StudentUserId).ToList(),
            interventionClient,
            bearerToken);

        var reviewedByAssessment = new Dictionary<Guid, int>();
        var recentAssessments = assessments
            .Select(item => new ClassDashboardAssessmentSummary(
                item.Id,
                item.Title,
                item.Status,
                item.DueAt,
                item.SubmissionCount,
                reviewedByAssessment.GetValueOrDefault(item.Id)))
            .ToList();

        return Results.Ok(new ClassLeadershipSummaryResponse(
            ToClass(schoolClass, principal),
            schoolClass.Enrollments.Select(e => new ClassMemberResponse(e.StudentUserId)).ToList(),
            recentAssessments,
            activeInterventions,
            eiInsights));
    }

    private static async Task<IResult> GetLeadershipInterventionMonitoring(
        Guid organisationId,
        Guid? yearLevelId,
        Guid? classId,
        string? severity,
        string? status,
        ClaimsPrincipal principal,
        OrganisationDbContext db,
        IInterventionDashboardClient interventionClient,
        IEiInsightsClient eiClient,
        HttpContext httpContext)
    {
        var access = await EvaluateLeadershipAccessAsync(principal, organisationId, db);
        if (access is not null)
        {
            return access;
        }

        if (await db.Organisations.FindAsync(organisationId) is null)
        {
            return Results.NotFound();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        var classes = await LoadClassesWithYearLevelsAsync(db, organisationId, yearLevelId);
        if (classId.HasValue)
        {
            classes = classes.Where(c => c.Id == classId.Value).ToList();
        }

        var studentClassLookup = BuildStudentClassLookup(classes);
        var gapSeverityLookup = await BuildGapSeverityLookupAsync(classes, eiClient, bearerToken);

        var interventions = await interventionClient.ListOrganisationInterventionsAsync(
            organisationId,
            status,
            bearerToken);

        var items = new List<LeadershipInterventionItem>();
        foreach (var intervention in interventions)
        {
            if (!studentClassLookup.TryGetValue(intervention.StudentUserId, out var classInfo))
            {
                continue;
            }

            gapSeverityLookup.TryGetValue(intervention.LearningGapId, out var gapSeverity);

            if (!string.IsNullOrWhiteSpace(severity)
                && !string.Equals(gapSeverity, severity.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            items.Add(new LeadershipInterventionItem(
                intervention.Id,
                intervention.StudentUserId,
                intervention.LearningGapId,
                intervention.AssignedTeacherUserId,
                intervention.PlannedActions,
                intervention.Outcome,
                intervention.Status,
                intervention.PlannedStartAt,
                intervention.PlannedEndAt,
                intervention.ReviewAt,
                intervention.CreatedAt,
                classInfo.ClassId,
                classInfo.ClassName,
                classInfo.YearLevelId,
                classInfo.YearLevelName,
                gapSeverity));
        }

        return Results.Ok(new LeadershipInterventionMonitoringResponse(
            organisationId,
            items.OrderByDescending(i => i.CreatedAt).ToList()));
    }

    private static Dictionary<string, (Guid ClassId, string ClassName, Guid YearLevelId, string YearLevelName)>
        BuildStudentClassLookup(IReadOnlyList<SchoolClass> classes)
    {
        var lookup = new Dictionary<string, (Guid, string, Guid, string)>();
        foreach (var schoolClass in classes)
        {
            foreach (var enrollment in schoolClass.Enrollments)
            {
                lookup[enrollment.StudentUserId] = (
                    schoolClass.Id,
                    schoolClass.Name,
                    schoolClass.YearLevelId,
                    schoolClass.YearLevel.Name);
            }
        }

        return lookup;
    }

    private static async Task<Dictionary<Guid, string>> BuildGapSeverityLookupAsync(
        IReadOnlyList<SchoolClass> classes,
        IEiInsightsClient eiClient,
        string bearerToken)
    {
        var lookup = new Dictionary<Guid, string>();
        foreach (var schoolClass in classes)
        {
            var insights = await eiClient.GetClassInsightsAsync(
                schoolClass.OrganisationId,
                schoolClass.Id,
                bearerToken);
            if (insights is null)
            {
                continue;
            }

            foreach (var gap in insights.ActiveLearningGaps)
            {
                lookup[gap.GapId] = gap.Severity;
            }
        }

        return lookup;
    }

    private static async Task<IResult?> EvaluateLeadershipAccessAsync(
        ClaimsPrincipal principal,
        Guid organisationId,
        OrganisationDbContext db)
    {
        if (principal.IsAdmin())
        {
            return null;
        }

        if (!principal.IsSchoolLeader())
        {
            return Results.Forbid();
        }

        var userId = principal.UserId();
        var assigned = await db.OrganisationLeaders.AnyAsync(
            leader => leader.OrganisationId == organisationId && leader.LeaderUserId == userId);
        return assigned ? null : Results.Forbid();
    }

    private static async Task<List<SchoolClass>> LoadClassesWithYearLevelsAsync(
        OrganisationDbContext db,
        Guid organisationId,
        Guid? yearLevelId = null)
    {
        var query = db.Classes
            .Include(c => c.Enrollments)
            .Include(c => c.YearLevel)
            .Where(c => c.OrganisationId == organisationId);

        if (yearLevelId.HasValue)
        {
            query = query.Where(c => c.YearLevelId == yearLevelId.Value);
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    private static async Task<IReadOnlyList<ClassAggregationResult>> AggregateClassesAsync(
        IReadOnlyList<SchoolClass> classes,
        IAssessmentDashboardClient assessmentClient,
        IInterventionDashboardClient interventionClient,
        IEiInsightsClient eiClient,
        string bearerToken)
    {
        var organisationId = classes.FirstOrDefault()?.OrganisationId;
        var plannedOrActive = organisationId is null
            ? []
            : await ListPlannedOrActiveInterventionsAsync(
                organisationId.Value,
                interventionClient,
                bearerToken);

        var tasks = classes.Select(async schoolClass =>
        {
            var studentIds = schoolClass.Enrollments.Select(e => e.StudentUserId).ToList();
            var assessments = await assessmentClient.GetRecentClassAssessmentSummariesAsync(
                schoolClass.OrganisationId,
                schoolClass.Id,
                bearerToken);
            var eiInsights = await eiClient.GetClassInsightsAsync(
                schoolClass.OrganisationId,
                schoolClass.Id,
                bearerToken);
            var activeInterventions = CountForStudents(plannedOrActive, studentIds);

            return LeadershipDashboardAggregator.AggregateClass(new ClassAggregationInput(
                schoolClass.Id,
                schoolClass.Name,
                schoolClass.YearLevelId,
                schoolClass.YearLevel.Name,
                studentIds,
                assessments,
                activeInterventions,
                eiInsights));
        });

        return await Task.WhenAll(tasks);
    }

    private static async Task<int> CountActiveInterventionsAsync(
        Guid organisationId,
        IReadOnlyList<string> studentUserIds,
        IInterventionDashboardClient interventionClient,
        string bearerToken)
    {
        var interventions = await ListPlannedOrActiveInterventionsAsync(
            organisationId,
            interventionClient,
            bearerToken);
        return CountForStudents(interventions, studentUserIds);
    }

    private static async Task<IReadOnlyList<InterventionDetailData>> ListPlannedOrActiveInterventionsAsync(
        Guid organisationId,
        IInterventionDashboardClient interventionClient,
        string bearerToken)
    {
        var interventions = await interventionClient.ListOrganisationInterventionsAsync(
            organisationId,
            status: null,
            bearerToken);
        return interventions
            .Where(item =>
                string.Equals(item.Status, "Planned", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static int CountForStudents(
        IReadOnlyList<InterventionDetailData> interventions,
        IReadOnlyList<string> studentUserIds)
    {
        var students = studentUserIds.ToHashSet(StringComparer.Ordinal);
        return interventions.Count(item => students.Contains(item.StudentUserId));
    }

    private static ClassResponse ToClass(SchoolClass schoolClass, ClaimsPrincipal principal) =>
        new(
            schoolClass.Id,
            schoolClass.OrganisationId,
            schoolClass.YearLevelId,
            schoolClass.Name,
            schoolClass.Code,
            schoolClass.Teachers.Select(t => t.TeacherUserId).ToList(),
            schoolClass.Enrollments.Select(e => e.StudentUserId).ToList());

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? string.Empty;

    private static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SystemAdministrator);

    private static bool IsSchoolLeader(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SchoolLeader);

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
