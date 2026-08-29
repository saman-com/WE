namespace EvidenceService.Domain;

public sealed class EducationalEvidence
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid ClassId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid SubmissionId { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = EvidenceStatuses.Approved;
    public string ApprovedByTeacherUserId { get; set; } = string.Empty;
    public DateTimeOffset ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<EvidenceMicroSkillMark> MicroSkillMarks { get; set; } = [];
}
