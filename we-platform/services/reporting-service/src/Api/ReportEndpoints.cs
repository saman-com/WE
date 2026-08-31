using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ReportingService.Application;
using ReportingService.Domain;
using ReportingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace ReportingService.Api;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/reports").RequireAuthorization();

        api.MapPost(
            "/organisations/{organisationId:guid}/classes/{classId:guid}/class-progress",
            GenerateClassProgressReport);
        api.MapPost(
            "/organisations/{organisationId:guid}/school-summary",
            GenerateSchoolSummaryReport);
        api.MapGet("/{reportId:guid}", GetReport);
        api.MapGet("/{reportId:guid}/pdf", ExportReportPdf);
    }

    private static async Task<IResult> GenerateClassProgressReport(
        Guid organisationId,
        Guid classId,
        ClaimsPrincipal principal,
        IOrganisationAccessChecker accessChecker,
        IReportGenerator reportGenerator,
        ITenantContext tenantContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (!tenantContext.HasTenant || organisationId != tenantContext.TenantId)
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        if (principal.IsTeacher())
        {
            var allowed = await accessChecker.TeacherCanManageClassAsync(
                principal.UserId(),
                organisationId,
                classId,
                bearerToken,
                cancellationToken);
            if (!allowed)
            {
                return Results.Forbid();
            }
        }

        var report = await reportGenerator.GenerateClassProgressReportAsync(
            organisationId,
            classId,
            principal.UserId(),
            bearerToken,
            cancellationToken);

        return report is null ? Results.NotFound() : Results.Ok(report);
    }

    private static async Task<IResult> GenerateSchoolSummaryReport(
        Guid organisationId,
        ClaimsPrincipal principal,
        IOrganisationAccessChecker accessChecker,
        IReportGenerator reportGenerator,
        ITenantContext tenantContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsSchoolLeader() && !principal.IsAdmin())
        {
            return Results.Forbid();
        }

        if (!tenantContext.HasTenant || organisationId != tenantContext.TenantId)
        {
            return Results.Forbid();
        }

        var bearerToken = ExtractBearerToken(httpContext.Request.Headers.Authorization.ToString());
        if (bearerToken is null)
        {
            return Results.Forbid();
        }

        if (principal.IsSchoolLeader())
        {
            var allowed = await accessChecker.SchoolLeaderCanViewOrganisationAsync(
                principal.UserId(),
                organisationId,
                bearerToken,
                cancellationToken);
            if (!allowed)
            {
                return Results.Forbid();
            }
        }

        var report = await reportGenerator.GenerateSchoolSummaryReportAsync(
            organisationId,
            principal.UserId(),
            bearerToken,
            cancellationToken);

        return report is null ? Results.NotFound() : Results.Ok(report);
    }

    private static async Task<IResult> GetReport(
        Guid reportId,
        ClaimsPrincipal principal,
        ReportingDbContext db,
        ITenantContext tenantContext,
        IReportGenerator reportGenerator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher() && !principal.IsSchoolLeader() && !principal.IsAdmin())
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

        var reportEntity = await db.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (reportEntity is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, reportEntity);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        var report = await reportGenerator.GetReportAsync(
            reportId,
            principal.UserId(),
            bearerToken,
            cancellationToken);

        return report is null ? Results.NotFound() : Results.Ok(report);
    }

    private static async Task<IResult> ExportReportPdf(
        Guid reportId,
        ClaimsPrincipal principal,
        ReportingDbContext db,
        ITenantContext tenantContext,
        IReportGenerator reportGenerator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!principal.IsTeacher() && !principal.IsSchoolLeader() && !principal.IsAdmin())
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

        var reportEntity = await db.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (reportEntity is null)
        {
            return Results.NotFound();
        }

        var tenantAccess = TenantAccess.ValidateEntityAccess(tenantContext, reportEntity);
        if (tenantAccess is not null)
        {
            return tenantAccess;
        }

        var pdfBytes = await reportGenerator.ExportReportPdfAsync(
            reportId,
            principal.UserId(),
            bearerToken,
            cancellationToken);

        return pdfBytes is null
            ? Results.NotFound()
            : Results.File(pdfBytes, "application/pdf", $"report-{reportId}.pdf");
    }

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
}
