namespace WePlatform.Tenancy;

/// <summary>
/// Stable MemoryCache key formats for organisation access checks.
/// Every key includes the school (organisation) id so a cached allow/deny
/// can never be reused across schools. Cache duration is 30 seconds — permission
/// changes (enrolment, class assignment) may take up to that long to take effect.
/// </summary>
public static class AccessCacheKeys
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(30);

    public static string TeacherClass(Guid schoolId, string teacherUserId, Guid classId) =>
        $"access:school:{schoolId}:teacher:{teacherUserId}:class:{classId}";

    public static string StudentClass(Guid schoolId, string studentUserId, Guid classId) =>
        $"access:school:{schoolId}:student:{studentUserId}:class:{classId}";

    public static string TeacherStudent(Guid schoolId, string teacherUserId, string studentUserId) =>
        $"access:school:{schoolId}:teacher:{teacherUserId}:student:{studentUserId}";

    public static string ParentStudent(Guid schoolId, string parentUserId, string studentUserId) =>
        $"access:school:{schoolId}:parent:{parentUserId}:student:{studentUserId}";

    public static string LeaderOrganisation(Guid schoolId, string schoolLeaderUserId) =>
        $"access:school:{schoolId}:leader:{schoolLeaderUserId}";

    public static string LeaderClass(Guid schoolId, string schoolLeaderUserId, Guid classId) =>
        $"access:school:{schoolId}:leader:{schoolLeaderUserId}:class:{classId}";

    public static string LeaderStudent(Guid schoolId, string schoolLeaderUserId, string studentUserId) =>
        $"access:school:{schoolId}:leader:{schoolLeaderUserId}:student:{studentUserId}";

    public static bool IncludesSchool(string cacheKey, Guid schoolId) =>
        cacheKey.Contains($"school:{schoolId}", StringComparison.Ordinal);
}
