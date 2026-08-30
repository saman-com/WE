using System.Net.Http.Headers;
using System.Net.Http.Json;
using EiService.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EiService.Infrastructure.Insights;

public interface IStudentEiDataClient
{
    Task<StudentEiSnapshot?> GetStudentSnapshotAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public sealed class HttpStudentEiDataClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpStudentEiDataClient> logger) : IStudentEiDataClient
{
    public async Task<StudentEiSnapshot?> GetStudentSnapshotAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var masteryTask = FetchMasteryAsync(studentUserId, bearerToken, cancellationToken);
        var gapsTask = FetchGapsAsync(studentUserId, bearerToken, cancellationToken);
        var diagnosticsTask = FetchDiagnosticsAsync(studentUserId, bearerToken, cancellationToken);

        await Task.WhenAll(masteryTask, gapsTask, diagnosticsTask);

        var mastery = await masteryTask;
        var gaps = await gapsTask;
        var diagnostics = await diagnosticsTask;

        if (mastery is null && gaps is null && diagnostics is null)
        {
            logger.LogWarning("Unable to load EI data for student {StudentUserId}.", studentUserId);
            return null;
        }

        return new StudentEiSnapshot(
            studentUserId,
            mastery ?? [],
            gaps ?? [],
            diagnostics ?? []);
    }

    private async Task<IReadOnlyList<StudentMasterySnapshot>?> FetchMasteryAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Mastery:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return [];
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/mastery/students/{studentUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<StudentMasteryResponse>(cancellationToken);
        return payload?.Records
            .Select(record => new StudentMasterySnapshot(
                record.MicroSkillId,
                record.MasteryLevel,
                record.Explanation))
            .ToList();
    }

    private async Task<IReadOnlyList<StudentGapSnapshot>?> FetchGapsAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Gaps:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return [];
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/gaps/students/{studentUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<StudentLearningGapsResponse>(cancellationToken);
        return payload?.Gaps
            .Select(gap => new StudentGapSnapshot(
                gap.Id,
                gap.EvidenceId,
                gap.MicroSkillId,
                gap.Severity,
                gap.Urgency,
                gap.Explanation))
            .ToList();
    }

    private async Task<IReadOnlyList<StudentDiagnosticSnapshot>?> FetchDiagnosticsAsync(
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Diagnostics:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return [];
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/v1/diagnostics/students/{studentUserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<StudentDiagnosticsResponse>(cancellationToken);
        return payload?.Diagnostics
            .Select(diagnostic => new StudentDiagnosticSnapshot(
                diagnostic.EvidenceId,
                diagnostic.MicroSkillId,
                diagnostic.Status,
                diagnostic.Reason,
                diagnostic.CreatedAt))
            .ToList();
    }

    private sealed record MasteryRecordResponse(
        Guid Id,
        Guid MicroSkillId,
        string MasteryLevel,
        decimal WeightedAverage,
        decimal ConfidenceScore,
        int EvidenceCount,
        string Explanation,
        DateTimeOffset CalculatedAt);

    private sealed record StudentMasteryResponse(
        string StudentUserId,
        IReadOnlyList<MasteryRecordResponse> Records);

    private sealed record LearningGapResponse(
        Guid Id,
        Guid EvidenceId,
        Guid AssessmentId,
        Guid MicroSkillId,
        Guid? LearningObjectiveId,
        string ExpectedMastery,
        string ActualMastery,
        decimal Mark,
        string Severity,
        string Urgency,
        string Explanation,
        DateTimeOffset CreatedAt);

    private sealed record StudentLearningGapsResponse(
        string StudentUserId,
        IReadOnlyList<LearningGapResponse> Gaps);

    private sealed record MicroSkillDiagnosticResponse(
        Guid Id,
        Guid EvidenceId,
        Guid AssessmentId,
        Guid MicroSkillId,
        string Status,
        decimal Mark,
        string Reason,
        DateTimeOffset CreatedAt);

    private sealed record StudentDiagnosticsResponse(
        string StudentUserId,
        IReadOnlyList<MicroSkillDiagnosticResponse> Diagnostics);
}
