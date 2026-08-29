using EvidenceService.Application;
using EvidenceService.Domain;
using WePlatform.Events;

namespace EvidenceService.Infrastructure.Messaging;

public static class EvidenceApprovalEventFactory
{
    public static EvidenceCreated CreateEvidenceCreated(EducationalEvidence evidence)
    {
        var eventId = Guid.CreateVersion7();
        return new EvidenceCreated(
            eventId,
            eventId,
            evidence.ApprovedAt,
            evidence.OrganisationId,
            EvidenceCreated.CurrentVersion,
            evidence.Id,
            evidence.AssessmentId,
            evidence.SubmissionId,
            evidence.StudentUserId,
            evidence.ClassId,
            evidence.ApprovedByTeacherUserId,
            evidence.ApprovedAt,
            evidence.MicroSkillMarks
                .Select(mark => new MicroSkillResult(mark.MicroSkillId, mark.Mark, mark.Feedback))
                .ToList());
    }

    public static AssessmentApproved CreateAssessmentApproved(EducationalEvidence evidence)
    {
        var eventId = Guid.CreateVersion7();
        return new AssessmentApproved(
            eventId,
            eventId,
            evidence.ApprovedAt,
            evidence.OrganisationId,
            AssessmentApproved.CurrentVersion,
            evidence.AssessmentId,
            evidence.StudentUserId,
            evidence.SubmissionId,
            evidence.Id,
            evidence.ApprovedByTeacherUserId,
            evidence.ApprovedAt,
            evidence.MicroSkillMarks
                .Select(mark => new MicroSkillResult(mark.MicroSkillId, mark.Mark, mark.Feedback))
                .ToList());
    }
}
