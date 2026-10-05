using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StudentLearningService.Application;
using StudentLearningService.Domain;
using StudentLearningService.Infrastructure.Data;
using WePlatform.Tenancy;

namespace StudentLearningService.Api;

public static class StudentLearningEndpoints
{
    public static void MapStudentLearningEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/students").RequireAuthorization();

        api.MapGet("/profiles/summaries", GetProfileSummaries);
        api.MapGet("/{studentUserId}/profile", GetProfile);
        api.MapGet("/{studentUserId}/profile/summary", GetProfileSummary);
        api.MapPost("/{studentUserId}/profile/enrollments", SyncEnrollment);
        api.MapPost("/{studentUserId}/profile/evidence", RecordEvidence);
    }

    private static async Task<IResult> GetProfileSummaries(
        [AsParameters] ProfileSummariesQuery query,
        ClaimsPrincipal principal,
        StudentLearningDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var ids = (query.StudentUserIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (ids.Count == 0)
        {
            return Results.Ok(Array.Empty<StudentProfileSummaryResponse>());
        }

        if (ids.Count > 200)
        {
            return Results.BadRequest();
        }

        foreach (var studentUserId in ids)
        {
            var access = await EvaluateProfileAccessAsync(
                principal,
                studentUserId,
                accessChecker,
                httpContext.Request.Headers.Authorization.ToString());
            if (access is not null)
            {
                return access;
            }
        }

        // Projection — avoid loading full evidence collections for summary counts.
        var summaries = await db.Profiles
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => ids.Contains(p.StudentUserId) && p.TenantId == tenantContext.TenantId)
            .Select(p => new StudentProfileSummaryResponse(
                p.StudentUserId,
                p.EvidenceEntries.Count,
                p.EvidenceEntries
                    .OrderByDescending(e => e.RecordedAt)
                    .Select(e => (DateTimeOffset?)e.RecordedAt)
                    .FirstOrDefault()))
            .ToListAsync();

        return Results.Ok(summaries);
    }

    private sealed record ProfileSummariesQuery(string[]? StudentUserIds);

    private static async Task<IResult> GetProfile(
        string studentUserId,
        ClaimsPrincipal principal,
        StudentLearningDbContext db,
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

        var access = await EvaluateProfileAccessAsync(
            principal,
            studentUserId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var profile = await db.Profiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Enrollments)
            .Include(p => p.EvidenceEntries
                .OrderByDescending(e => e.RecordedAt)
                .Take(50))
            .FirstOrDefaultAsync(p => p.StudentUserId == studentUserId);

        if (profile is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, profile);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        return Results.Ok(ToResponse(profile));
    }

    private static async Task<IResult> GetProfileSummary(
        string studentUserId,
        ClaimsPrincipal principal,
        StudentLearningDbContext db,
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

        var access = await EvaluateProfileAccessAsync(
            principal,
            studentUserId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        var profile = await db.Profiles
            .IgnoreQueryFilters()
            .Include(p => p.EvidenceEntries)
            .FirstOrDefaultAsync(p => p.StudentUserId == studentUserId);

        if (profile is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, profile);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        return Results.Ok(ToSummaryResponse(profile));
    }

    private static async Task<IResult> SyncEnrollment(
        string studentUserId,
        SyncProfileEnrollmentRequest request,
        ClaimsPrincipal principal,
        StudentLearningDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.IsAdmin())
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

        if (string.IsNullOrWhiteSpace(studentUserId)
            || string.IsNullOrWhiteSpace(request.ClassName)
            || string.IsNullOrWhiteSpace(request.ClassCode))
        {
            return Results.BadRequest();
        }

        var profile = await db.Profiles
            .Include(p => p.Enrollments)
            .FirstOrDefaultAsync(p => p.StudentUserId == studentUserId);

        var now = DateTimeOffset.UtcNow;
        if (profile is null)
        {
            profile = new StudentLearningProfile
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantContext.TenantId!.Value,
                StudentUserId = studentUserId,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync();
        }

        if (!await db.ProfileEnrollments.AnyAsync(e =>
                e.ProfileId == profile.Id && e.ClassId == request.ClassId))
        {
            db.ProfileEnrollments.Add(new ProfileClassEnrollment
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantContext.TenantId!.Value,
                ProfileId = profile.Id,
                OrganisationId = request.OrganisationId,
                ClassId = request.ClassId,
                ClassName = request.ClassName.Trim(),
                ClassCode = request.ClassCode.Trim(),
                EnrolledAt = now
            });
        }

        profile.UpdatedAt = now;
        await db.SaveChangesAsync();

        var saved = await db.Profiles
            .Include(p => p.Enrollments)
            .Include(p => p.EvidenceEntries)
            .FirstAsync(p => p.Id == profile.Id);

        return Results.Ok(ToResponse(saved));
    }

    private static async Task<IResult> RecordEvidence(
        string studentUserId,
        RecordProfileEvidenceRequest request,
        ClaimsPrincipal principal,
        StudentLearningDbContext db,
        IOrganisationAccessChecker accessChecker,
        ITenantContext tenantContext,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(studentUserId) || string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest();
        }

        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var access = await EvaluateProfileAccessAsync(
            principal,
            studentUserId,
            accessChecker,
            httpContext.Request.Headers.Authorization.ToString());
        if (access is not null)
        {
            return access;
        }

        if (principal.IsStudent())
        {
            return Results.Forbid();
        }

        var profile = await db.Profiles
            .IgnoreQueryFilters()
            .Include(p => p.Enrollments)
            .Include(p => p.EvidenceEntries)
                .ThenInclude(e => e.MicroSkills)
            .FirstOrDefaultAsync(p => p.StudentUserId == studentUserId);
        if (profile is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, profile);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        if (profile.EvidenceEntries.Any(e => e.Id == request.EvidenceId))
        {
            return Results.Ok(ToResponse(profile));
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new ProfileEvidenceEntry
        {
            Id = request.EvidenceId,
            ProfileId = profile.Id,
            AssessmentId = request.AssessmentId,
            Title = request.Title.Trim(),
            RecordedAt = request.RecordedAt
        };

        foreach (var microSkillId in (request.MicroSkillIds ?? []).Distinct())
        {
            entry.MicroSkills.Add(new ProfileEvidenceMicroSkill
            {
                EvidenceEntryId = request.EvidenceId,
                MicroSkillId = microSkillId
            });
        }

        db.EvidenceEntries.Add(entry);
        profile.UpdatedAt = now;
        await db.SaveChangesAsync();

        var saved = await db.Profiles
            .Include(p => p.Enrollments)
            .Include(p => p.EvidenceEntries)
            .FirstAsync(p => p.Id == profile.Id);

        return Results.Ok(ToResponse(saved));
    }

    private static async Task<IResult?> EvaluateProfileAccessAsync(
        ClaimsPrincipal principal,
        string studentUserId,
        IOrganisationAccessChecker accessChecker,
        string authorizationHeader)
    {
        if (principal.IsAdmin())
        {
            return null;
        }

        var userId = principal.UserId();
        if (principal.IsStudent())
        {
            return userId == studentUserId ? null : Results.Forbid();
        }

        if (principal.IsTeacher())
        {
            var token = ExtractBearerToken(authorizationHeader);
            if (token is null)
            {
                return Results.Forbid();
            }

            var allowed = await accessChecker.TeacherCanViewStudentAsync(userId, studentUserId, token);
            return allowed ? null : Results.Forbid();
        }

        return Results.Forbid();
    }

    private static StudentProfileSummaryResponse ToSummaryResponse(StudentLearningProfile profile)
    {
        var latest = profile.EvidenceEntries
            .OrderByDescending(e => e.RecordedAt)
            .FirstOrDefault();

        return new StudentProfileSummaryResponse(
            profile.StudentUserId,
            profile.EvidenceEntries.Count,
            latest?.RecordedAt);
    }

    private static StudentProfileResponse ToResponse(StudentLearningProfile profile) =>
        new(
            profile.StudentUserId,
            profile.Enrollments
                .OrderBy(e => e.EnrolledAt)
                .Select(e => new ClassEnrollmentSummary(
                    e.OrganisationId,
                    e.ClassId,
                    e.ClassName,
                    e.ClassCode,
                    e.EnrolledAt))
                .ToList(),
            profile.EvidenceEntries
                .OrderBy(e => e.RecordedAt)
                .Select(e => new EvidenceTimelineEntry(e.Id, e.Title, e.RecordedAt))
                .ToList());

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

    private static bool IsStudent(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.Student);
}
