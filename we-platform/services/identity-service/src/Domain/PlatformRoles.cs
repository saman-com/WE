namespace IdentityService.Domain;

public static class PlatformRoles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Parent = "Parent";
    public const string SchoolLeader = "SchoolLeader";
    public const string SystemAdministrator = "SystemAdministrator";

    public static readonly string[] All =
    [
        Student,
        Teacher,
        Parent,
        SchoolLeader,
        SystemAdministrator
    ];
}
