namespace OrganisationService.Application;

public sealed record EvidenceSummaryData(Guid AssessmentId, int ReviewedCount);

public interface IEvidenceDashboardClient
{
    Task<IReadOnlyList<EvidenceSummaryData>> ListClassEvidenceSummariesAsync(
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
