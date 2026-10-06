using System.Net;
using System.Net.Http.Json;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantIsolationEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static TheoryData<string, string> OrganisationPathRoutes => new()
    {
        { "DELETE", "/api/v1/organisations/{organisationId}" },
        { "GET", "/api/v1/organisations/{organisationId}" },
        { "PUT", "/api/v1/organisations/{organisationId}" },
        { "GET", "/api/v1/organisations/{organisationId}/classes" },
        { "POST", "/api/v1/organisations/{organisationId}/classes" },
        { "DELETE", "/api/v1/organisations/{organisationId}/classes/{classId}" },
        { "GET", "/api/v1/organisations/{organisationId}/classes/{classId}" },
        { "PUT", "/api/v1/organisations/{organisationId}/classes/{classId}" },
        { "GET", "/api/v1/organisations/{organisationId}/classes/{classId}/dashboard" },
        { "GET", "/api/v1/organisations/{organisationId}/classes/{classId}/enrollments" },
        { "POST", "/api/v1/organisations/{organisationId}/classes/{classId}/enrollments" },
        { "DELETE", "/api/v1/organisations/{organisationId}/classes/{classId}/enrollments/{userId}" },
        { "GET", "/api/v1/organisations/{organisationId}/classes/{classId}/leadership/summary" },
        { "GET", "/api/v1/organisations/{organisationId}/classes/{classId}/teachers" },
        { "POST", "/api/v1/organisations/{organisationId}/classes/{classId}/teachers" },
        { "DELETE", "/api/v1/organisations/{organisationId}/classes/{classId}/teachers/{userId}" },
        { "POST", "/api/v1/organisations/{organisationId}/leaders" },
        { "DELETE", "/api/v1/organisations/{organisationId}/leaders/{userId}" },
        { "GET", "/api/v1/organisations/{organisationId}/leadership/dashboard" },
        { "GET", "/api/v1/organisations/{organisationId}/leadership/interventions" },
        { "GET", "/api/v1/organisations/{organisationId}/year-levels" },
        { "POST", "/api/v1/organisations/{organisationId}/year-levels" },
        { "DELETE", "/api/v1/organisations/{organisationId}/year-levels/{yearLevelId}" },
        { "GET", "/api/v1/organisations/{organisationId}/year-levels/{yearLevelId}" },
        { "PUT", "/api/v1/organisations/{organisationId}/year-levels/{yearLevelId}" },
        { "GET", "/api/v1/organisations/{organisationId}/year-levels/{yearLevelId}/leadership/dashboard" }
    };

    [Theory]
    [MemberData(nameof(OrganisationPathRoutes))]
    public async Task CrossSchool_OrganisationPath_BothDirections_Denied(string method, string template)
    {
        // Probe: organisation-service:organisation-path
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var orgA = await CreateOrganisationAsAdminAsync(adminA, "School A", UniqueCode("SCA"));
        var orgB = await CreateOrganisationAsAdminAsync(adminB, "School B", UniqueCode("SCB"));

        var yearLevelId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var userId = Guid.NewGuid().ToString();

        await AssertDeniedAsync(
            method,
            Expand(template, orgA.Id, yearLevelId, classId, userId),
            adminB,
            orgB.Id);
        await AssertDeniedAsync(
            method,
            Expand(template, orgB.Id, yearLevelId, classId, userId),
            adminA,
            orgA.Id);
    }

    [Fact]
    public async Task CrossSchool_OrganisationList_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: organisation-service:organisation-list
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var orgA = await CreateOrganisationAsAdminAsync(adminA, "List School A", UniqueCode("LSA"));
        var orgB = await CreateOrganisationAsAdminAsync(adminB, "List School B", UniqueCode("LSB"));

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/organisations",
            adminA,
            orgA.Id,
            TestJwt.AdminRole);
        var okA = await _client.SendAsync(listA);
        okA.EnsureSuccessStatusCode();
        var scopedA = await okA.Content.ReadFromJsonAsync<List<OrganisationResponse>>();
        Assert.Contains(scopedA!, item => item.Id == orgA.Id);
        Assert.DoesNotContain(scopedA!, item => item.Id == orgB.Id);

        using var listB = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/organisations",
            adminB,
            orgB.Id,
            TestJwt.AdminRole);
        var okB = await _client.SendAsync(listB);
        okB.EnsureSuccessStatusCode();
        var scopedB = await okB.Content.ReadFromJsonAsync<List<OrganisationResponse>>();
        Assert.Contains(scopedB!, item => item.Id == orgB.Id);
        Assert.DoesNotContain(scopedB!, item => item.Id == orgA.Id);
    }

    public static TheoryData<string, string, string> PersonResourceRoutes => new()
    {
        { "GET", "/api/v1/students/{studentUserId}/workspace", TestJwt.StudentRole },
        { "GET", "/api/v1/parents/me/children/{studentUserId}/progress", TestJwt.ParentRole },
        { "GET", "/api/v1/parents/{parentUserId}/children", TestJwt.ParentRole },
        { "GET", "/api/v1/access/parent/{parentUserId}/student/{studentUserId}", TestJwt.ParentRole },
        { "GET", "/api/v1/access/teacher/{teacherUserId}/student/{studentUserId}", TestJwt.TeacherRole }
    };

    [Theory]
    [MemberData(nameof(PersonResourceRoutes))]
    public async Task CrossSchool_PersonResource_BothDirections_Denied(string method, string template, string role)
    {
        // Probe: organisation-service:person-resource
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        var parentA = Guid.NewGuid().ToString();
        var parentB = Guid.NewGuid().ToString();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();

        var orgA = await CreateOrganisationAsAdminAsync(adminA, "Person School A", UniqueCode("PSA"));
        var orgB = await CreateOrganisationAsAdminAsync(adminB, "Person School B", UniqueCode("PSB"));
        var yearA = await CreateYearLevelAsAdminAsync(adminA, orgA.Id, "Year 7", 7);
        var yearB = await CreateYearLevelAsAdminAsync(adminB, orgB.Id, "Year 7", 7);
        var classA = await CreateClassAsAdminAsync(adminA, orgA.Id, yearA.Id, "7A", UniqueCode("7A"));
        var classB = await CreateClassAsAdminAsync(adminB, orgB.Id, yearB.Id, "7B", UniqueCode("7B"));
        await EnrollStudentAsync(adminA, orgA.Id, classA.Id, studentA);
        await EnrollStudentAsync(adminB, orgB.Id, classB.Id, studentB);
        await AssignTeacherAsync(adminA, orgA.Id, classA.Id, teacherA);
        await AssignTeacherAsync(adminB, orgB.Id, classB.Id, teacherB);
        await LinkParentToStudentAsync(adminA, orgA.Id, parentA, studentA);
        await LinkParentToStudentAsync(adminB, orgB.Id, parentB, studentB);

        var (callerB, tenantB) = RoleContext(role, studentB, parentB, teacherB, orgB.Id);
        var pathIntoA = ExpandPerson(template, studentA, parentA, teacherA);
        await AssertDeniedAsync(method, pathIntoA, callerB, tenantB, role);

        var (callerA, tenantA) = RoleContext(role, studentA, parentA, teacherA, orgA.Id);
        var pathIntoB = ExpandPerson(template, studentB, parentB, teacherB);
        await AssertDeniedAsync(method, pathIntoB, callerA, tenantA, role);
    }

    [Fact]
    public async Task CrossSchool_AdminCannotLinkParentToOtherSchoolStudent()
    {
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var parentB = Guid.NewGuid().ToString();

        var orgA = await CreateOrganisationAsAdminAsync(adminA, "Link School A", UniqueCode("LKA"));
        var yearA = await CreateYearLevelAsAdminAsync(adminA, orgA.Id, "Year 8", 8);
        var classA = await CreateClassAsAdminAsync(adminA, orgA.Id, yearA.Id, "8A", UniqueCode("8A"));
        await EnrollStudentAsync(adminA, orgA.Id, classA.Id, studentA);
        var orgB = await CreateOrganisationAsAdminAsync(adminB, "Link School B", UniqueCode("LKB"));

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/parents/{parentB}/children",
            adminB,
            orgB.Id,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new LinkParentStudentRequest(studentA));
        var response = await _client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"POST parent link returned {response.StatusCode}");
    }

    [Fact]
    public async Task CrossSchool_ParentClassTeachers_BothDirections_NeverReturnsTheOtherSchool()
    {
        // Probe: organisation-service:parent-teachers
        var adminA = Guid.NewGuid().ToString();
        var adminB = Guid.NewGuid().ToString();
        var parentA = Guid.NewGuid().ToString();
        var parentB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();

        var orgA = await CreateOrganisationAsAdminAsync(adminA, "Teacher Scope A", UniqueCode("TSA"));
        var orgB = await CreateOrganisationAsAdminAsync(adminB, "Teacher Scope B", UniqueCode("TSB"));
        var yearA = await CreateYearLevelAsAdminAsync(adminA, orgA.Id, "Year 9", 9);
        var yearB = await CreateYearLevelAsAdminAsync(adminB, orgB.Id, "Year 9", 9);
        var classA = await CreateClassAsAdminAsync(adminA, orgA.Id, yearA.Id, "9A", UniqueCode("9A"));
        var classB = await CreateClassAsAdminAsync(adminB, orgB.Id, yearB.Id, "9B", UniqueCode("9B"));
        await EnrollStudentAsync(adminA, orgA.Id, classA.Id, studentA);
        await EnrollStudentAsync(adminB, orgB.Id, classB.Id, studentB);
        await AssignTeacherAsync(adminA, orgA.Id, classA.Id, teacherA);
        await AssignTeacherAsync(adminB, orgB.Id, classB.Id, teacherB);
        await LinkParentToStudentAsync(adminA, orgA.Id, parentA, studentA);
        await LinkParentToStudentAsync(adminB, orgB.Id, parentB, studentB);

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/parents/me/class-teachers",
            parentA,
            orgA.Id,
            TestJwt.ParentRole);
        var okA = await _client.SendAsync(listA);
        okA.EnsureSuccessStatusCode();
        var scopedA = await okA.Content.ReadFromJsonAsync<List<string>>();
        Assert.Contains(teacherA, scopedA!);
        Assert.DoesNotContain(teacherB, scopedA!);

        using var listB = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/parents/me/class-teachers",
            parentB,
            orgB.Id,
            TestJwt.ParentRole);
        var okB = await _client.SendAsync(listB);
        okB.EnsureSuccessStatusCode();
        var scopedB = await okB.Content.ReadFromJsonAsync<List<string>>();
        Assert.Contains(teacherB, scopedB!);
        Assert.DoesNotContain(teacherA, scopedB!);
    }

    private static (string UserId, Guid TenantId) RoleContext(
        string role,
        string studentId,
        string parentId,
        string teacherId,
        Guid tenantId) =>
        role switch
        {
            TestJwt.StudentRole => (studentId, tenantId),
            TestJwt.ParentRole => (parentId, tenantId),
            TestJwt.TeacherRole => (teacherId, tenantId),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

    private async Task AssertDeniedAsync(string method, string path, string userId, Guid tenantId, string role = TestJwt.AdminRole)
    {
        using var request = TestJwt.Authorized(new HttpMethod(method), path, userId, tenantId, role);
        AttachBody(request, method, path);

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"{method} {path} as {role} returned {response.StatusCode}");
    }

    private static void AttachBody(HttpRequestMessage request, string method, string path)
    {
        if (method is not ("PUT" or "POST"))
        {
            return;
        }

        request.Content = path switch
        {
            var p when p.EndsWith("/year-levels", StringComparison.Ordinal) =>
                JsonContent.Create(new CreateYearLevelRequest("Probe Year", 1)),
            var p when p.Contains("/classes/", StringComparison.Ordinal) && p.EndsWith("/teachers", StringComparison.Ordinal) =>
                JsonContent.Create(new AssignTeacherRequest(Guid.NewGuid().ToString())),
            var p when p.Contains("/classes/", StringComparison.Ordinal) && p.EndsWith("/enrollments", StringComparison.Ordinal) =>
                JsonContent.Create(new EnrollStudentRequest(Guid.NewGuid().ToString())),
            var p when p.Contains("/classes", StringComparison.Ordinal) && method == "POST" =>
                JsonContent.Create(new CreateClassRequest("Probe Class", UniqueCode("CLS"), Guid.CreateVersion7())),
            var p when p.Contains("/classes/", StringComparison.Ordinal) && method == "PUT" =>
                JsonContent.Create(new UpdateClassRequest("Probe Class", UniqueCode("CLS"), Guid.CreateVersion7())),
            var p when p.EndsWith("/leaders", StringComparison.Ordinal) =>
                JsonContent.Create(new AssignSchoolLeaderRequest(Guid.NewGuid().ToString())),
            var p when p.Contains("/year-levels/", StringComparison.Ordinal) && method == "PUT" =>
                JsonContent.Create(new CreateYearLevelRequest("Probe Year", 1)),
            _ => JsonContent.Create(new UpdateOrganisationRequest("Probe Org", UniqueCode("PRB")))
        };
    }

    private static string Expand(
        string template,
        Guid targetOrganisationId,
        Guid yearLevelId,
        Guid classId,
        string userId) =>
        template
            .Replace("{organisationId}", targetOrganisationId.ToString(), StringComparison.Ordinal)
            .Replace("{yearLevelId}", yearLevelId.ToString(), StringComparison.Ordinal)
            .Replace("{classId}", classId.ToString(), StringComparison.Ordinal)
            .Replace("{userId}", userId, StringComparison.Ordinal);

    private static string ExpandPerson(string template, string studentUserId, string parentUserId, string teacherUserId) =>
        template
            .Replace("{studentUserId}", studentUserId, StringComparison.Ordinal)
            .Replace("{parentUserId}", parentUserId, StringComparison.Ordinal)
            .Replace("{teacherUserId}", teacherUserId, StringComparison.Ordinal);

    private async Task<OrganisationResponse> CreateOrganisationAsAdminAsync(string adminId, string name, string code)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/organisations", adminId, TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest(name, code));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<OrganisationResponse>())!;
        return created;
    }

    private async Task<YearLevelResponse> CreateYearLevelAsAdminAsync(
        string adminId,
        Guid organisationId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/year-levels",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateYearLevelRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<YearLevelResponse>())!;
    }

    private async Task<ClassResponse> CreateClassAsAdminAsync(
        string adminId,
        Guid organisationId,
        Guid yearLevelId,
        string name,
        string code)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateClassRequest(name, code, yearLevelId));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClassResponse>())!;
    }

    private async Task EnrollStudentAsync(string adminId, Guid organisationId, Guid classId, string studentId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/enrollments",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new EnrollStudentRequest(studentId));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task AssignTeacherAsync(string adminId, Guid organisationId, Guid classId, string teacherId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/teachers",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new AssignTeacherRequest(teacherId));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task LinkParentToStudentAsync(string adminId, Guid organisationId, string parentId, string studentId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/parents/{parentId}/children",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new LinkParentStudentRequest(studentId));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
