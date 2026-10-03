namespace CurriculumService.Domain;

public static class CurriculumScopes
{
    public const string Regional = "Regional";
    public const string School = "School";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Regional,
        School
    };
}
