namespace CurriculumService.Domain;

public static class CurriculumStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Archived = "Archived";

    public static readonly string[] All = [Draft, Active, Archived];
}
