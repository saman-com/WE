namespace NationalReportingService.Domain;

public static class NationalApiScopes
{
    public const string Enrollment = "national:enrollment";
    public const string Mastery = "national:mastery";
    public const string Coverage = "national:coverage";

    public static readonly IReadOnlyList<string> All =
    [
        Enrollment,
        Mastery,
        Coverage
    ];
}
