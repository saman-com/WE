using WePlatform.Tenancy;

namespace WePlatform.AspNetCore.Tests;

public sealed class AccessCacheKeysTests
{
    [Fact]
    public void AllKeys_IncludeSchoolId_AndDifferAcrossSchools()
    {
        var schoolA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var schoolB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var classId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var keysA = new[]
        {
            AccessCacheKeys.TeacherClass(schoolA, "teacher-1", classId),
            AccessCacheKeys.StudentClass(schoolA, "student-1", classId),
            AccessCacheKeys.TeacherStudent(schoolA, "teacher-1", "student-1"),
            AccessCacheKeys.ParentStudent(schoolA, "parent-1", "student-1"),
            AccessCacheKeys.LeaderOrganisation(schoolA, "leader-1"),
            AccessCacheKeys.LeaderClass(schoolA, "leader-1", classId),
        };

        var keysB = new[]
        {
            AccessCacheKeys.TeacherClass(schoolB, "teacher-1", classId),
            AccessCacheKeys.StudentClass(schoolB, "student-1", classId),
            AccessCacheKeys.TeacherStudent(schoolB, "teacher-1", "student-1"),
            AccessCacheKeys.ParentStudent(schoolB, "parent-1", "student-1"),
            AccessCacheKeys.LeaderOrganisation(schoolB, "leader-1"),
            AccessCacheKeys.LeaderClass(schoolB, "leader-1", classId),
        };

        Assert.Equal(keysA.Length, keysB.Length);
        for (var i = 0; i < keysA.Length; i++)
        {
            Assert.True(AccessCacheKeys.IncludesSchool(keysA[i], schoolA));
            Assert.False(AccessCacheKeys.IncludesSchool(keysA[i], schoolB));
            Assert.True(AccessCacheKeys.IncludesSchool(keysB[i], schoolB));
            Assert.NotEqual(keysA[i], keysB[i]);
        }
    }

    [Fact]
    public void DefaultDuration_IsThirtySeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), AccessCacheKeys.DefaultDuration);
    }
}
