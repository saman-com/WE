using System.Text.Json;
using AssessmentService.Application;
using AssessmentService.Domain;
using AssessmentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssessmentService.Infrastructure.Ai;

public sealed class AiFeedbackDraftService(
    AssessmentDbContext db,
    IAiGatewayClient aiGatewayClient)
{
    private const string PromptId = "assessment-feedback";
    private const string PromptVersion = "1.0.0";
    private const string ContextScope = "teacher-review";

    public async Task<AiFeedbackDraftResponse?> RequestDraftAsync(
        Guid assessmentId,
        Guid submissionId,
        Guid microSkillId,
        string teacherUserId,
        CancellationToken cancellationToken = default)
    {
        var assessment = await db.Assessments
            .Include(a => a.MicroSkills)
            .FirstOrDefaultAsync(a => a.Id == assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        if (!assessment.MicroSkills.Any(m => m.MicroSkillId == microSkillId))
        {
            return null;
        }

        var submission = await db.Submissions.FirstOrDefaultAsync(
            s => s.Id == submissionId && s.AssessmentId == assessmentId,
            cancellationToken);
        if (submission is null)
        {
            return null;
        }

        var variables = new Dictionary<string, string>
        {
            ["studentName"] = submission.StudentUserId,
            ["microSkillName"] = microSkillId.ToString(),
            ["evidenceSummary"] = submission.Responses
        };

        var completion = await aiGatewayClient.CompleteAsync(
            new AiCompletionRequest(PromptId, PromptVersion, ContextScope, variables),
            cancellationToken);
        if (completion is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var auditLog = new AiFeedbackAuditLog
        {
            Id = Guid.CreateVersion7(),
            AssessmentId = assessmentId,
            SubmissionId = submissionId,
            MicroSkillId = microSkillId,
            TeacherUserId = teacherUserId,
            PromptId = completion.PromptId,
            PromptVersion = completion.PromptVersion,
            PromptVariablesJson = JsonSerializer.Serialize(variables),
            AiResponse = completion.Content,
            CreatedAt = now
        };

        db.AiFeedbackAuditLogs.Add(auditLog);
        await db.SaveChangesAsync(cancellationToken);

        return new AiFeedbackDraftResponse(
            auditLog.Id,
            completion.Content,
            completion.PromptId,
            completion.PromptVersion);
    }

    public async Task<FinalizeAiFeedbackAuditResponse?> FinalizeAuditAsync(
        Guid auditLogId,
        string teacherUserId,
        string teacherEditedFeedback,
        Guid evidenceId,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await db.AiFeedbackAuditLogs.FindAsync([auditLogId], cancellationToken);
        if (auditLog is null || auditLog.TeacherUserId != teacherUserId)
        {
            return null;
        }

        if (auditLog.FinalApprovedAt.HasValue)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        auditLog.TeacherEditedFeedback = teacherEditedFeedback.Trim();
        auditLog.EvidenceId = evidenceId;
        auditLog.FinalApprovedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return new FinalizeAiFeedbackAuditResponse(auditLog.Id, now);
    }
}
