using NationalReportingService.Api.Auth;
using NationalReportingService.Application;
using Microsoft.AspNetCore.Http.HttpResults;

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
            .Produces<NationalEnrollmentResponse>()
            .WithOpenApi();

        api.MapGet("/mastery-benchmarks", GetMasteryBenchmarks)
            .RequireAuthorization(NationalAuthorizationPolicies.Mastery)
            .WithName("GetNationalMasteryBenchmarks")
            .Produces<NationalMasteryBenchmarksResponse>()
            .WithOpenApi();

        api.MapGet("/curriculum-coverage", GetCurriculumCoverage)
            .RequireAuthorization(NationalAuthorizationPolicies.Coverage)
            .WithName("GetNationalCurriculumCoverage")
            .Produces<NationalCurriculumCoverageResponse>()
            .WithOpenApi();
    }

    private static async Task<Ok<NationalEnrollmentResponse>> GetEnrollment(
        INationalReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetEnrollmentAsync(cancellationToken);
        return TypedResults.Ok(report);
    }

    private static async Task<Ok<NationalMasteryBenchmarksResponse>> GetMasteryBenchmarks(
        INationalReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetMasteryBenchmarksAsync(cancellationToken);
        return TypedResults.Ok(report);
    }

    private static async Task<Ok<NationalCurriculumCoverageResponse>> GetCurriculumCoverage(
        INationalReportQuery query,
        CancellationToken cancellationToken)
    {
        var report = await query.GetCurriculumCoverageAsync(cancellationToken);
        return TypedResults.Ok(report);
    }
}
