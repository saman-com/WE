using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrganisationService.Application;
using OrganisationService.Domain;
using OrganisationService.Infrastructure.Data;

namespace OrganisationService.Api;

public static class OrganisationEndpoints
{
    public static void MapOrganisationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/organisations").RequireAuthorization();

        api.MapGet("/", ListOrganisations);
        api.MapPost("/", CreateOrganisation);
        api.MapGet("/{organisationId:guid}", GetOrganisation);
        api.MapPut("/{organisationId:guid}", UpdateOrganisation);
        api.MapDelete("/{organisationId:guid}", DeleteOrganisation);

        api.MapGet("/{organisationId:guid}/year-levels", ListYearLevels);
        api.MapPost("/{organisationId:guid}/year-levels", CreateYearLevel);
        api.MapGet("/{organisationId:guid}/year-levels/{yearLevelId:guid}", GetYearLevel);
        api.MapPut("/{organisationId:guid}/year-levels/{yearLevelId:guid}", UpdateYearLevel);
        api.MapDelete("/{organisationId:guid}/year-levels/{yearLevelId:guid}", DeleteYearLevel);

        api.MapGet("/{organisationId:guid}/classes", ListClasses);
        api.MapPost("/{organisationId:guid}/classes", CreateClass);
        api.MapGet("/{organisationId:guid}/classes/{classId:guid}", GetClass);
        api.MapPut("/{organisationId:guid}/classes/{classId:guid}", UpdateClass);
        api.MapDelete("/{organisationId:guid}/classes/{classId:guid}", DeleteClass);

        api.MapGet("/{organisationId:guid}/classes/{classId:guid}/teachers", ListTeachers);
        api.MapPost("/{organisationId:guid}/classes/{classId:guid}/teachers", AssignTeacher);
        api.MapDelete("/{organisationId:guid}/classes/{classId:guid}/teachers/{userId}", UnassignTeacher);

        api.MapGet("/{organisationId:guid}/classes/{classId:guid}/enrollments", ListEnrollments);
        api.MapPost("/{organisationId:guid}/classes/{classId:guid}/enrollments", EnrollStudent);
        api.MapDelete("/{organisationId:guid}/classes/{classId:guid}/enrollments/{userId}", UnenrollStudent);
    }

    private static async Task<IResult> ListOrganisations(
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        var query = db.Organisations.AsQueryable();
        if (!principal.IsAdmin())
        {
            var userId = principal.UserId();
            if (principal.IsTeacher())
            {
                query = query.Where(org => org.Classes.Any(c => c.Teachers.Any(t => t.TeacherUserId == userId)));
            }
            else if (principal.IsStudent())
            {
                query = query.Where(org => org.Classes.Any(c => c.Enrollments.Any(e => e.StudentUserId == userId)));
            }
            else
            {
                return Results.Forbid();
            }
        }

        var items = await query
            .OrderBy(org => org.Name)
            .Select(org => new OrganisationResponse(org.Id, org.Name, org.Code))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateOrganisation(
        CreateOrganisationRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        if (await db.Organisations.AnyAsync(org => org.Code == request.Code))
        {
            return Results.Conflict();
        }

        var organisation = new Organisation
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Organisations.Add(organisation);
        await db.SaveChangesAsync();

        var response = ToOrganisation(organisation);
        return Results.Created($"/api/v1/organisations/{organisation.Id}", response);
    }

    private static async Task<IResult> GetOrganisation(
        Guid organisationId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        var organisation = await db.Organisations.FindAsync(organisationId);
        if (organisation is null)
        {
            return Results.NotFound();
        }

        if (!await CanViewOrganisationAsync(principal, organisationId, db))
        {
            return Results.Forbid();
        }

        return Results.Ok(ToOrganisation(organisation));
    }

    private static async Task<IResult> UpdateOrganisation(
        Guid organisationId,
        UpdateOrganisationRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var organisation = await db.Organisations.FindAsync(organisationId);
        if (organisation is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        if (await db.Organisations.AnyAsync(org => org.Code == request.Code && org.Id != organisationId))
        {
            return Results.Conflict();
        }

        organisation.Name = request.Name.Trim();
        organisation.Code = request.Code.Trim();
        await db.SaveChangesAsync();
        return Results.Ok(ToOrganisation(organisation));
    }

    private static async Task<IResult> DeleteOrganisation(
        Guid organisationId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var organisation = await db.Organisations.FindAsync(organisationId);
        if (organisation is null)
        {
            return Results.NotFound();
        }

        db.Organisations.Remove(organisation);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListYearLevels(
        Guid organisationId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (await db.Organisations.FindAsync(organisationId) is null)
        {
            return Results.NotFound();
        }

        if (!await CanViewOrganisationAsync(principal, organisationId, db))
        {
            return Results.Forbid();
        }

        var items = await db.YearLevels
            .Where(level => level.OrganisationId == organisationId)
            .OrderBy(level => level.SortOrder)
            .Select(level => new YearLevelResponse(level.Id, level.OrganisationId, level.Name, level.SortOrder))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateYearLevel(
        Guid organisationId,
        CreateYearLevelRequest request,
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

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        var yearLevel = new YearLevel
        {
            Id = Guid.CreateVersion7(),
            OrganisationId = organisationId,
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder
        };

        db.YearLevels.Add(yearLevel);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/organisations/{organisationId}/year-levels/{yearLevel.Id}",
            ToYearLevel(yearLevel));
    }

    private static async Task<IResult> GetYearLevel(
        Guid organisationId,
        Guid yearLevelId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        var yearLevel = await FindYearLevelAsync(db, organisationId, yearLevelId);
        if (yearLevel is null)
        {
            return Results.NotFound();
        }

        if (!await CanViewOrganisationAsync(principal, organisationId, db))
        {
            return Results.Forbid();
        }

        return Results.Ok(ToYearLevel(yearLevel));
    }

    private static async Task<IResult> UpdateYearLevel(
        Guid organisationId,
        Guid yearLevelId,
        UpdateYearLevelRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var yearLevel = await FindYearLevelAsync(db, organisationId, yearLevelId);
        if (yearLevel is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest();
        }

        yearLevel.Name = request.Name.Trim();
        yearLevel.SortOrder = request.SortOrder;
        await db.SaveChangesAsync();
        return Results.Ok(ToYearLevel(yearLevel));
    }

    private static async Task<IResult> DeleteYearLevel(
        Guid organisationId,
        Guid yearLevelId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var yearLevel = await FindYearLevelAsync(db, organisationId, yearLevelId);
        if (yearLevel is null)
        {
            return Results.NotFound();
        }

        db.YearLevels.Remove(yearLevel);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListClasses(
        Guid organisationId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (await db.Organisations.FindAsync(organisationId) is null)
        {
            return Results.NotFound();
        }

        var query = db.Classes
            .Include(c => c.Teachers)
            .Include(c => c.Enrollments)
            .Where(c => c.OrganisationId == organisationId);

        if (!principal.IsAdmin())
        {
            var userId = principal.UserId();
            if (principal.IsTeacher())
            {
                query = query.Where(c => c.Teachers.Any(t => t.TeacherUserId == userId));
            }
            else if (principal.IsStudent())
            {
                query = query.Where(c => c.Enrollments.Any(e => e.StudentUserId == userId));
            }
            else
            {
                return Results.Forbid();
            }
        }

        var items = await query
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Results.Ok(items.Select(c => ToClass(c, principal)).ToList());
    }

    private static async Task<IResult> CreateClass(
        Guid organisationId,
        CreateClassRequest request,
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

        var yearLevel = await FindYearLevelAsync(db, organisationId, request.YearLevelId);
        if (yearLevel is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        var schoolClass = new SchoolClass
        {
            Id = Guid.CreateVersion7(),
            OrganisationId = organisationId,
            YearLevelId = yearLevel.Id,
            Name = request.Name.Trim(),
            Code = request.Code.Trim()
        };

        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/organisations/{organisationId}/classes/{schoolClass.Id}",
            ToClass(schoolClass, principal));
    }

    private static async Task<IResult> GetClass(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        var access = await EvaluateClassAccessAsync(principal, schoolClass, db);
        return access ?? Results.Ok(ToClass(schoolClass, principal));
    }

    private static async Task<IResult> UpdateClass(
        Guid organisationId,
        Guid classId,
        UpdateClassRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        var yearLevel = await FindYearLevelAsync(db, organisationId, request.YearLevelId);
        if (yearLevel is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        schoolClass.Name = request.Name.Trim();
        schoolClass.Code = request.Code.Trim();
        schoolClass.YearLevelId = yearLevel.Id;
        await db.SaveChangesAsync();
        return Results.Ok(ToClass(schoolClass, principal));
    }

    private static async Task<IResult> DeleteClass(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        db.Classes.Remove(schoolClass);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListTeachers(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        var access = await EvaluateClassAccessAsync(principal, schoolClass, db);
        if (access is not null)
        {
            return access;
        }

        var teachers = schoolClass.Teachers
            .Select(t => new ClassMemberResponse(t.TeacherUserId))
            .ToList();
        return Results.Ok(teachers);
    }

    private static async Task<IResult> AssignTeacher(
        Guid organisationId,
        Guid classId,
        AssignTeacherRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.BadRequest();
        }

        if (schoolClass.Teachers.Any(t => t.TeacherUserId == request.UserId))
        {
            return Results.Conflict();
        }

        db.ClassTeachers.Add(new ClassTeacher
        {
            ClassId = schoolClass.Id,
            TeacherUserId = request.UserId
        });
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/organisations/{organisationId}/classes/{classId}/teachers/{request.UserId}",
            new ClassMemberResponse(request.UserId));
    }

    private static async Task<IResult> UnassignTeacher(
        Guid organisationId,
        Guid classId,
        string userId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var assignment = await db.ClassTeachers.FirstOrDefaultAsync(
            t => t.ClassId == classId && t.TeacherUserId == userId);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        db.ClassTeachers.Remove(assignment);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ListEnrollments(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        var access = await EvaluateClassAccessAsync(principal, schoolClass, db);
        if (access is not null)
        {
            return access;
        }

        IEnumerable<ClassEnrollment> enrollments = schoolClass.Enrollments;
        if (principal.IsStudent() && !principal.IsAdmin())
        {
            var userId = principal.UserId();
            enrollments = enrollments.Where(e => e.StudentUserId == userId);
        }

        return Results.Ok(enrollments.Select(e => new ClassMemberResponse(e.StudentUserId)).ToList());
    }

    private static async Task<IResult> EnrollStudent(
        Guid organisationId,
        Guid classId,
        EnrollStudentRequest request,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.BadRequest();
        }

        if (schoolClass.Enrollments.Any(e => e.StudentUserId == request.UserId))
        {
            return Results.Conflict();
        }

        db.ClassEnrollments.Add(new ClassEnrollment
        {
            ClassId = schoolClass.Id,
            StudentUserId = request.UserId
        });
        await db.SaveChangesAsync();
        return Results.Created(
            $"/api/v1/organisations/{organisationId}/classes/{classId}/enrollments/{request.UserId}",
            new ClassMemberResponse(request.UserId));
    }

    private static async Task<IResult> UnenrollStudent(
        Guid organisationId,
        Guid classId,
        string userId,
        ClaimsPrincipal principal,
        OrganisationDbContext db)
    {
        if (!principal.IsAdmin())
        {
            return Results.Forbid();
        }

        var schoolClass = await FindClassAsync(db, organisationId, classId);
        if (schoolClass is null)
        {
            return Results.NotFound();
        }

        var enrollment = schoolClass.Enrollments.FirstOrDefault(e => e.StudentUserId == userId);
        if (enrollment is null)
        {
            return Results.NotFound();
        }

        db.ClassEnrollments.Remove(enrollment);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult?> EvaluateClassAccessAsync(
        ClaimsPrincipal principal,
        SchoolClass schoolClass,
        OrganisationDbContext db)
    {
        if (principal.IsAdmin())
        {
            return null;
        }

        var userId = principal.UserId();
        if (principal.IsTeacher())
        {
            var assigned = schoolClass.Teachers.Any(t => t.TeacherUserId == userId)
                || await db.ClassTeachers.AnyAsync(t => t.ClassId == schoolClass.Id && t.TeacherUserId == userId);
            return assigned ? null : Results.Forbid();
        }

        if (principal.IsStudent())
        {
            var enrolled = schoolClass.Enrollments.Any(e => e.StudentUserId == userId)
                || await db.ClassEnrollments.AnyAsync(e => e.ClassId == schoolClass.Id && e.StudentUserId == userId);
            return enrolled ? null : Results.Forbid();
        }

        return Results.Forbid();
    }

    private static async Task<bool> CanViewOrganisationAsync(
        ClaimsPrincipal principal,
        Guid organisationId,
        OrganisationDbContext db)
    {
        if (principal.IsAdmin())
        {
            return true;
        }

        var userId = principal.UserId();
        if (principal.IsTeacher())
        {
            return await db.ClassTeachers.AnyAsync(t =>
                t.Class.OrganisationId == organisationId && t.TeacherUserId == userId);
        }

        if (principal.IsStudent())
        {
            return await db.ClassEnrollments.AnyAsync(e =>
                e.Class.OrganisationId == organisationId && e.StudentUserId == userId);
        }

        return false;
    }

    private static async Task<YearLevel?> FindYearLevelAsync(
        OrganisationDbContext db,
        Guid organisationId,
        Guid yearLevelId) =>
        await db.YearLevels.FirstOrDefaultAsync(level =>
            level.Id == yearLevelId && level.OrganisationId == organisationId);

    private static async Task<SchoolClass?> FindClassAsync(
        OrganisationDbContext db,
        Guid organisationId,
        Guid classId) =>
        await db.Classes
            .Include(c => c.Teachers)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == classId && c.OrganisationId == organisationId);

    private static OrganisationResponse ToOrganisation(Organisation organisation) =>
        new(organisation.Id, organisation.Name, organisation.Code);

    private static YearLevelResponse ToYearLevel(YearLevel yearLevel) =>
        new(yearLevel.Id, yearLevel.OrganisationId, yearLevel.Name, yearLevel.SortOrder);

    private static ClassResponse ToClass(SchoolClass schoolClass, ClaimsPrincipal principal)
    {
        var includeStudents = principal.IsAdmin() || principal.IsTeacher();
        return new ClassResponse(
            schoolClass.Id,
            schoolClass.OrganisationId,
            schoolClass.YearLevelId,
            schoolClass.Name,
            schoolClass.Code,
            schoolClass.Teachers.Select(t => t.TeacherUserId).ToList(),
            includeStudents ? schoolClass.Enrollments.Select(e => e.StudentUserId).ToList() : null);
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
