using System.Text.Json;
using EiService.Application;
using EiService.Domain;
using EiService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace EiService.Infrastructure.Ai;

public sealed class AiSummaryDraftService(
    EiDbContext db,
    ITenantContext tenantContext,
    IAiGatewayClient aiGatewayClient,
    IClassInsightsProvider insightsProvider,
    IStudentEiDataClient studentEiDataClient,
    ISummaryContextBuilder contextBuilder)
{
    private const string LessonSummaryPromptVersion = "1.0.0";
    private const string ProgressReportPromptVersion = "1.0.0";
    private const string LessonSummaryContextScope = "class-lesson-summary";
    private const string ProgressReportContextScope = "student-progress-report";

    public async Task<AiSummaryDraftResponse?> RequestLessonSummaryDraftAsync(
        Guid organisationId,
        Guid classId,
        Guid unitId,
        string teacherUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (unitId == Guid.Empty)
        {
            return null;
        }

        var insights = await insightsProvider.GetClassInsightsAsync(
            organisationId,
            classId,
            bearerToken,
            cancellationToken);
        if (insights is null)
        {
            return null;
        }

        var variables = contextBuilder.BuildLessonSummaryVariables(insights, unitId);
        var completion = await aiGatewayClient.CompleteAsync(
            new AiCompletionRequest(
                AiSummaryTypes.LessonSummary,
                LessonSummaryPromptVersion,
                LessonSummaryContextScope,
                variables),
            cancellationToken);
        if (completion is null)
        {
            return null;
        }

        return await PersistDraftAsync(
            AiSummaryTypes.LessonSummary,
            organisationId,
            classId,
            unitId,
            null,
            teacherUserId,
            variables,
            LessonSummaryContextScope,
            completion,
            cancellationToken);
    }

    public async Task<AiSummaryDraftResponse?> RequestProgressReportDraftAsync(
        string studentUserId,
        Guid organisationId,
        Guid classId,
        string teacherUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentUserId))
        {
            return null;
        }

        var snapshot = await studentEiDataClient.GetStudentSnapshotAsync(
            studentUserId,
            bearerToken,
            cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        var variables = contextBuilder.BuildProgressReportVariables(snapshot);
        var completion = await aiGatewayClient.CompleteAsync(
            new AiCompletionRequest(
                AiSummaryTypes.ProgressReportNarrative,
                ProgressReportPromptVersion,
                ProgressReportContextScope,
                variables),
            cancellationToken);
        if (completion is null)
        {
            return null;
        }

        return await PersistDraftAsync(
            AiSummaryTypes.ProgressReportNarrative,
            organisationId,
            classId,
            null,
            studentUserId,
            teacherUserId,
            variables,
            ProgressReportContextScope,
            completion,
            cancellationToken);
    }

    public async Task<FinalizeAiSummaryAuditResponse?> FinalizeAuditAsync(
        Guid auditLogId,
        string teacherUserId,
        string teacherEditedContent,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await db.AiSummaryAuditLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == auditLogId, cancellationToken);
        if (auditLog is null || auditLog.TeacherUserId != teacherUserId)
        {
            return null;
        }

        if (TenantAccess.ValidateEntityAccess(tenantContext, auditLog) is not null)
        {
            return null;
        }

        if (auditLog.FinalApprovedAt.HasValue)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(teacherEditedContent))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        auditLog.TeacherEditedContent = teacherEditedContent.Trim();
        auditLog.FinalApprovedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return new FinalizeAiSummaryAuditResponse(auditLog.Id, now, IsAiAssistedDraft: false);
    }

    private async Task<AiSummaryDraftResponse> PersistDraftAsync(
        string summaryType,
        Guid organisationId,
        Guid classId,
        Guid? unitId,
        string? studentUserId,
        string teacherUserId,
        IReadOnlyDictionary<string, string> variables,
        string contextScope,
        AiCompletionResult completion,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var auditLog = new AiSummaryAuditLog
        {
            Id = Guid.CreateVersion7(),
            TenantId = organisationId,
            SummaryType = summaryType,
            OrganisationId = organisationId,
            ClassId = classId,
            UnitId = unitId,
            StudentUserId = studentUserId,
            TeacherUserId = teacherUserId,
            PromptId = completion.PromptId,
            PromptVersion = completion.PromptVersion,
            PromptVariablesJson = JsonSerializer.Serialize(variables),
            ContextScopeJson = JsonSerializer.Serialize(new { scope = contextScope }),
            AiResponse = completion.Content,
            CreatedAt = now
        };

        db.AiSummaryAuditLogs.Add(auditLog);
        await db.SaveChangesAsync(cancellationToken);

        return new AiSummaryDraftResponse(
            auditLog.Id,
            completion.Content,
            completion.PromptId,
            completion.PromptVersion,
            IsAiAssistedDraft: true);
    }
}
