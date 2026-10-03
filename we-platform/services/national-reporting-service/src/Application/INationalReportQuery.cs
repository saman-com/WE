namespace NationalReportingService.Application;

public interface INationalReportQuery
{
    Task<NationalEnrollmentResponse> GetEnrollmentAsync(CancellationToken cancellationToken = default);

    Task<NationalMasteryBenchmarksResponse> GetMasteryBenchmarksAsync(
        CancellationToken cancellationToken = default);

    Task<NationalCurriculumCoverageResponse> GetCurriculumCoverageAsync(
        CancellationToken cancellationToken = default);
}
