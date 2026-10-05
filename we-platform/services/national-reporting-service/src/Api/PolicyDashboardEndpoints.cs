using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
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
            .Produces<PolicyTrendsResponse>()
            .WithOpenApi();

        api.MapGet("/equity", GetEquity)
            .WithName("GetPolicyDashboardEquity")
            .Produces<EquityAnalysisResponse>()
            .WithOpenApi();

        api.MapGet("/curriculum-effectiveness", GetCurriculumEffectiveness)
            .WithName("GetPolicyDashboardCurriculumEffectiveness")
            .Produces<CurriculumEffectivenessComparisonResponse>()
            .WithOpenApi();

        api.MapGet("/intervention-impact", GetInterventionImpact)
            .WithName("GetPolicyDashboardInterventionImpact")
            .Produces<InterventionImpactResponse>()
            .WithOpenApi();
    }

    private static async Task<Ok<PolicyTrendsResponse>> GetTrends(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetTrendsAsync(cancellationToken);
        return TypedResults.Ok(report);
    }

    private static async Task<Ok<EquityAnalysisResponse>> GetEquity(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetEquityAnalysisAsync(cancellationToken);
        return TypedResults.Ok(report);
    }

    private static async Task<Ok<CurriculumEffectivenessComparisonResponse>> GetCurriculumEffectiveness(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetCurriculumEffectivenessAsync(cancellationToken);
        return TypedResults.Ok(report);
    }

    private static async Task<Ok<InterventionImpactResponse>> GetInterventionImpact(
        IPolicyDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetInterventionImpactAsync(cancellationToken);
        return TypedResults.Ok(report);
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
