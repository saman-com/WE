namespace OrganisationService.Domain;

public sealed class ParentStudentLink
{
    public required string ParentUserId { get; set; }
    public required string StudentUserId { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
}
