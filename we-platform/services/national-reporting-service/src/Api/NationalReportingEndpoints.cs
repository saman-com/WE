using NationalReportingService.Api.Auth;
using NationalReportingService.Application;

namespace NationalReportingService.Api;

public static class NationalReportingEndpoints
{
    public static void MapNationalReportingEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/national")
            .RequireRateLimiting("national-api")
            .WithTags("National Reporting");

        api.MapGet("/enrollment", GetEnrollment)
            .RequireAuthorization(NationalAuthorizationPolicies.Enrollment)
            .WithName("GetNationalEnrollment")
            .WithOpenApi();

        api.MapGet("/mastery-benchmarks", GetMasteryBenchmarks)
            .RequireAuthorization(NationalAuthorizationPolicies.Mastery)
            .WithName("GetNationalMasteryBenchmarks")
            .WithOpenApi();

        api.MapGet("/curriculum-coverage", GetCurriculumCoverage)
            .RequireAuthorization(NationalAuthorizationPolicies.Coverage)
            .WithName("GetNationalCurriculumCoverage")
            .WithOpenApi();
    }

    private static async Task<IResult> GetEnrollment(
        INationalReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetEnrollmentAsync(cancellationToken);
        return Results.Ok(report);
    }

    private static async Task<IResult> GetMasteryBenchmarks(
        INationalReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetMasteryBenchmarksAsync(cancellationToken);
        return Results.Ok(report);
    }

    private static async Task<IResult> GetCurriculumCoverage(
        INationalReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetCurriculumCoverageAsync(cancellationToken);
        return Results.Ok(report);
    }
}
