using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FederationService.Application;
using FederationService.Domain;
using FederationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace FederationService.Api;

public static class FederationEndpoints
{
    public static void MapFederationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/federation").RequireAuthorization();

        api.MapGet("/schools", ListSchools);
        api.MapPost("/schools", CreateSchool);
        api.MapPost("/schools/{schoolTenantId:guid}/admins", AssignSchoolAdmin);
        api.MapGet("/metrics", GetMetrics);
        api.MapGet("/policies", ListPolicies);
        api.MapPut("/policies", UpdatePolicies);
    }

    private static async Task<IResult> ListSchools(ClaimsPrincipal principal, FederationDbContext db)
    {
        if (!TryGetFederationId(principal, out var federationId))
        {
            return Results.Forbid();
        }

        var schools = await db.FederationSchools
            .Where(school => school.FederationId == federationId)
            .OrderBy(school => school.Name)
            .Select(school => ToSchoolResponse(school))
            .ToListAsync();

        return Results.Ok(schools);
    }

    private static async Task<IResult> CreateSchool(
        CreateFederationSchoolRequest request,
        ClaimsPrincipal principal,
        FederationDbContext db,
        IRegionalConfigurationProvisioner provisioner)
    {
        if (!TryGetFederationId(principal, out var federationId))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        var normalizedCode = request.Code.Trim();
        if (await db.FederationSchools.AnyAsync(
                school => school.FederationId == federationId && school.Code == normalizedCode))
        {
            return Results.Conflict();
        }

        var tenantId = Guid.CreateVersion7();
        var school = new FederationSchool
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            FederationId = federationId,
            Name = request.Name.Trim(),
            Code = normalizedCode,
            EnrollmentCount = 0,
            AverageProgressPercent = 0m,
            HasDefaultConfiguration = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.FederationSchools.Add(school);
        await db.SaveChangesAsync();

        try
        {
            await provisioner.ProvisionDefaultConfigurationAsync(tenantId);
            school.HasDefaultConfiguration = true;
            await db.SaveChangesAsync();
        }
        catch (HttpRequestException)
        {
            school.HasDefaultConfiguration = false;
        }

        return Results.Created(
            $"/api/v1/federation/schools/{school.TenantId}",
            ToSchoolResponse(school));
    }

    private static async Task<IResult> AssignSchoolAdmin(
        Guid schoolTenantId,
        AssignSchoolAdminRequest request,
        ClaimsPrincipal principal,
        FederationDbContext db)
    {
        if (!TryGetFederationId(principal, out var federationId))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.BadRequest();
        }

        var school = await db.FederationSchools
            .FirstOrDefaultAsync(item =>
                item.FederationId == federationId && item.TenantId == schoolTenantId);
        if (school is null)
        {
            return Results.NotFound();
        }

        if (await db.SchoolAdminAssignments.AnyAsync(item =>
                item.SchoolTenantId == schoolTenantId && item.UserId == request.UserId))
        {
            return Results.Conflict();
        }

        var assignment = new SchoolAdminAssignment
        {
            Id = Guid.CreateVersion7(),
            FederationId = federationId,
            SchoolTenantId = schoolTenantId,
            UserId = request.UserId.Trim(),
            AssignedAt = DateTimeOffset.UtcNow
        };

        db.SchoolAdminAssignments.Add(assignment);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/v1/federation/schools/{schoolTenantId}/admins/{request.UserId}",
            new SchoolAdminAssignmentResponse(schoolTenantId, assignment.UserId, assignment.AssignedAt));
    }

    private static async Task<IResult> GetMetrics(ClaimsPrincipal principal, FederationDbContext db)
    {
        if (!TryGetFederationId(principal, out var federationId))
        {
            return Results.Forbid();
        }

        var schools = await db.FederationSchools
            .Where(school => school.FederationId == federationId)
            .OrderBy(school => school.Name)
            .ToListAsync();

        var summaries = schools
            .Select(school => new SchoolMetricSummary(
                school.TenantId,
                school.Name,
                school.EnrollmentCount,
                school.AverageProgressPercent))
            .ToList();

        var totalEnrollment = summaries.Sum(item => item.EnrollmentCount);
        var averageProgress = summaries.Count == 0
            ? 0m
            : Math.Round(summaries.Average(item => item.AverageProgressPercent), 2);

        return Results.Ok(new FederationMetricsResponse(
            summaries.Count,
            totalEnrollment,
            averageProgress,
            summaries));
    }

    private static async Task<IResult> ListPolicies(ClaimsPrincipal principal, FederationDbContext db)
    {
        if (!TryGetFederationId(principal, out var federationId))
        {
            return Results.Forbid();
        }

        var policies = await db.FederationPolicies
            .Where(policy => policy.FederationId == federationId)
            .OrderBy(policy => policy.PolicyKey)
            .Select(policy => new FederationPolicyDto(policy.PolicyKey, policy.PolicyValue))
            .ToListAsync();

        return Results.Ok(policies);
    }

    private static async Task<IResult> UpdatePolicies(
        UpdateFederationPoliciesRequest request,
        ClaimsPrincipal principal,
        FederationDbContext db)
    {
        if (!TryGetFederationId(principal, out var federationId))
        {
            return Results.Forbid();
        }

        if (request.Policies is null || request.Policies.Count == 0)
        {
            return Results.BadRequest();
        }

        foreach (var policy in request.Policies)
        {
            if (string.IsNullOrWhiteSpace(policy.PolicyKey) || string.IsNullOrWhiteSpace(policy.PolicyValue))
            {
                return Results.BadRequest();
            }

            var existing = await db.FederationPolicies.FirstOrDefaultAsync(item =>
                item.FederationId == federationId && item.PolicyKey == policy.PolicyKey.Trim());
            if (existing is null)
            {
                existing = new FederationPolicy
                {
                    Id = Guid.CreateVersion7(),
                    FederationId = federationId,
                    PolicyKey = policy.PolicyKey.Trim()
                };
                db.FederationPolicies.Add(existing);
            }

            existing.PolicyValue = policy.PolicyValue.Trim();
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        return await ListPolicies(principal, db);
    }

    private static bool TryGetFederationId(ClaimsPrincipal principal, out Guid federationId)
    {
        federationId = Guid.Empty;
        if (!principal.IsInRole(PlatformRoles.FederationAdmin))
        {
            return false;
        }

        var claim = principal.FindFirstValue(FederationClaimTypes.FederationId);
        return Guid.TryParse(claim, out federationId);
    }

    private static FederationSchoolResponse ToSchoolResponse(FederationSchool school) =>
        new(
            school.TenantId,
            school.FederationId,
            school.Name,
            school.Code,
            school.HasDefaultConfiguration,
            school.CreatedAt);
}
