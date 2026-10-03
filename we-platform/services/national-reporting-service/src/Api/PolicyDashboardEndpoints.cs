using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NationalReportingService.Application;
using NationalReportingService.Domain;

namespace NationalReportingService.Api;

public static class PolicyDashboardEndpoints
{
    public const string EducationAuthorityOfficerPolicy = "EducationAuthorityOfficer";

    public static void MapPolicyDashboardEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/policy-dashboards")
            .RequireAuthorization(EducationAuthorityOfficerPolicy)
            .WithTags("Policy Dashboards");

        api.MapGet("/trends", GetTrends)
            .WithName("GetPolicyDashboardTrends")
            .WithOpenApi();

        api.MapGet("/equity", GetEquity)
            .WithName("GetPolicyDashboardEquity")
            .WithOpenApi();

        api.MapGet("/curriculum-effectiveness", GetCurriculumEffectiveness)
            .WithName("GetPolicyDashboardCurriculumEffectiveness")
            .WithOpenApi();

        api.MapGet("/intervention-impact", GetInterventionImpact)
            .WithName("GetPolicyDashboardInterventionImpact")
            .WithOpenApi();
    }

    private static async Task<IResult> GetTrends(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetTrendsAsync(cancellationToken);
        return Results.Ok(report);
    }

    private static async Task<IResult> GetEquity(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetEquityAnalysisAsync(cancellationToken);
        return Results.Ok(report);
    }

    private static async Task<IResult> GetCurriculumEffectiveness(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetCurriculumEffectivenessAsync(cancellationToken);
        return Results.Ok(report);
    }

    private static async Task<IResult> GetInterventionImpact(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetInterventionImpactAsync(cancellationToken);
        return Results.Ok(report);
    }

    public static void AddEducationAuthorityOfficerPolicy(this AuthorizationOptions options)
    {
        options.AddPolicy(
            EducationAuthorityOfficerPolicy,
            policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(PlatformRoles.EducationAuthorityOfficer);
            });
    }
}
