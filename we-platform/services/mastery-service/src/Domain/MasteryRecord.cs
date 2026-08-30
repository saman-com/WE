namespace MasteryService.Domain;

public sealed class MasteryRecord
{
    public Guid Id { get; set; }
    public string StudentUserId { get; set; } = string.Empty;
    public Guid OrganisationId { get; set; }
    public Guid MicroSkillId { get; set; }
    public string MasteryLevel { get; set; } = string.Empty;
    public decimal WeightedAverage { get; set; }
    public decimal ConfidenceScore { get; set; }
    public int EvidenceCount { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; set; }
}
