namespace EiService.Application;

public interface IAiGatewayClient
{
    Task<AiCompletionResult?> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IStudentEiDataClient
{
    Task<StudentEiSnapshot?> GetStudentSnapshotAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public interface ISummaryContextBuilder
{
    IReadOnlyDictionary<string, string> BuildLessonSummaryVariables(
        ClassEiInsightsResponse insights,
        Guid unitId);

    IReadOnlyDictionary<string, string> BuildProgressReportVariables(
        StudentEiSnapshot snapshot);
}
