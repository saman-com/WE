using System.Net;
using System.Net.Http.Json;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public class OrganisationEndpointTests : IClassFixture<OrganisationWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrganisationEndpointTests(OrganisationWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/organisations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateOrganisation()
    {
        var teacherId = Guid.NewGuid().ToString();
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/organisations",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest("Denied School", UniqueCode("DEN")));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCrudOrganisationYearLevelAndClass()
    {
        var adminId = Guid.NewGuid().ToString();
        var code = UniqueCode("SCH");

        var createdOrg = await CreateOrganisationAsAdminAsync(adminId, "Riverside School", code);
        Assert.Equal("Riverside School", createdOrg.Name);
        Assert.Equal(code, createdOrg.Code);

        var listed = await SendAsAsync<List<OrganisationResponse>>(
            HttpMethod.Get,
            "/api/v1/organisations",
            adminId,
            TestJwt.AdminRole);
        Assert.Contains(listed, org => org.Id == createdOrg.Id);

        var fetched = await SendAsAsync<OrganisationResponse>(
            HttpMethod.Get,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            TestJwt.AdminRole);
        Assert.Equal(createdOrg.Id, fetched.Id);

        using var updateRequest = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            TestJwt.AdminRole);
        updateRequest.Content = JsonContent.Create(new UpdateOrganisationRequest("Riverside Primary", code));
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedOrg = await updateResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.Equal("Riverside Primary", updatedOrg!.Name);

        var yearLevel = await CreateYearLevelAsAdminAsync(adminId, createdOrg.Id, "Year 7", 7);
        Assert.Equal("Year 7", yearLevel.Name);
        Assert.Equal(createdOrg.Id, yearLevel.OrganisationId);

        using var updateYearRequest = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/organisations/{createdOrg.Id}/year-levels/{yearLevel.Id}",
            adminId,
            TestJwt.AdminRole);
        updateYearRequest.Content = JsonContent.Create(new UpdateYearLevelRequest("Year 8", 8));
        var updateYearResponse = await _client.SendAsync(updateYearRequest);
        Assert.Equal(HttpStatusCode.OK, updateYearResponse.StatusCode);

        var schoolClass = await CreateClassAsAdminAsync(adminId, createdOrg.Id, yearLevel.Id, "8A", UniqueCode("8A"));
        Assert.Equal("8A", schoolClass.Name);
        Assert.Equal(yearLevel.Id, schoolClass.YearLevelId);

        using var updateClassRequest = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/organisations/{createdOrg.Id}/classes/{schoolClass.Id}",
            adminId,
            TestJwt.AdminRole);
        updateClassRequest.Content = JsonContent.Create(
            new UpdateClassRequest("8B", schoolClass.Code, yearLevel.Id));
        var updateClassResponse = await _client.SendAsync(updateClassRequest);
        Assert.Equal(HttpStatusCode.OK, updateClassResponse.StatusCode);

        using var deleteClass = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/organisations/{createdOrg.Id}/classes/{schoolClass.Id}",
            adminId,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteClass)).StatusCode);

        using var deleteYear = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/organisations/{createdOrg.Id}/year-levels/{yearLevel.Id}",
            adminId,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteYear)).StatusCode);

        using var deleteOrg = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteOrg)).StatusCode);

        using var missing = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{createdOrg.Id}",
            adminId,
            TestJwt.AdminRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);
    }

    [Fact]
    public async Task Admin_CanAssignTeacherAndEnrollStudent()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var org = await CreateOrganisationAsAdminAsync(adminId, "Harbour School", UniqueCode("HAR"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 5", 5);
        var schoolClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "5A", UniqueCode("5A"));

        using var assign = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/teachers",
            adminId,
            TestJwt.AdminRole);
        assign.Content = JsonContent.Create(new AssignTeacherRequest(teacherId));
        var assignResponse = await _client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.Created, assignResponse.StatusCode);

        using var enroll = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/enrollments",
            adminId,
            TestJwt.AdminRole);
        enroll.Content = JsonContent.Create(new EnrollStudentRequest(studentId));
        var enrollResponse = await _client.SendAsync(enroll);
        Assert.Equal(HttpStatusCode.Created, enrollResponse.StatusCode);

        var teachers = await SendAsAsync<List<ClassMemberResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/teachers",
            adminId,
            TestJwt.AdminRole);
        Assert.Contains(teachers, member => member.UserId == teacherId);

        var enrollments = await SendAsAsync<List<ClassMemberResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{schoolClass.Id}/enrollments",
            adminId,
            TestJwt.AdminRole);
        Assert.Contains(enrollments, member => member.UserId == studentId);
    }

    [Fact]
    public async Task Teacher_SeesOnlyAssignedClasses()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Oakridge School", UniqueCode("OAK"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 3", 3);
        var assigned = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "3A", UniqueCode("3A"));
        var other = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "3B", UniqueCode("3B"));

        await AssignTeacherAsync(adminId, org.Id, assigned.Id, teacherId);
        await AssignTeacherAsync(adminId, org.Id, other.Id, otherTeacherId);

        var classes = await SendAsAsync<List<ClassResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes",
            teacherId,
            TestJwt.TeacherRole);

        Assert.Contains(classes, item => item.Id == assigned.Id);
        Assert.DoesNotContain(classes, item => item.Id == other.Id);
    }

    [Fact]
    public async Task Student_SeesOnlyOwnEnrollment()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Maple School", UniqueCode("MAP"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 4", 4);
        var enrolledClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "4A", UniqueCode("4A"));
        var otherClass = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "4B", UniqueCode("4B"));

        await EnrollStudentAsync(adminId, org.Id, enrolledClass.Id, studentId);
        await EnrollStudentAsync(adminId, org.Id, otherClass.Id, otherStudentId);

        var classes = await SendAsAsync<List<ClassResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes",
            studentId,
            TestJwt.StudentRole);

        Assert.Contains(classes, item => item.Id == enrolledClass.Id);
        Assert.DoesNotContain(classes, item => item.Id == otherClass.Id);

        var enrollments = await SendAsAsync<List<ClassMemberResponse>>(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{enrolledClass.Id}/enrollments",
            studentId,
            TestJwt.StudentRole);

        Assert.Single(enrollments);
        Assert.Equal(studentId, enrollments[0].UserId);
    }

    [Fact]
    public async Task Teacher_IsDeniedCrossClassAccess()
    {
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var otherTeacherId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Cedar School", UniqueCode("CED"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 6", 6);
        var assigned = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "6A", UniqueCode("6A"));
        var other = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "6B", UniqueCode("6B"));

        await AssignTeacherAsync(adminId, org.Id, assigned.Id, teacherId);
        await AssignTeacherAsync(adminId, org.Id, other.Id, otherTeacherId);

        using var classRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}",
            teacherId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(classRequest)).StatusCode);

        using var enrollmentsRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}/enrollments",
            teacherId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(enrollmentsRequest)).StatusCode);
    }

    [Fact]
    public async Task Student_IsDeniedCrossClassAccess()
    {
        var adminId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();

        var org = await CreateOrganisationAsAdminAsync(adminId, "Pine School", UniqueCode("PIN"));
        var year = await CreateYearLevelAsAdminAsync(adminId, org.Id, "Year 2", 2);
        var enrolled = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "2A", UniqueCode("2A"));
        var other = await CreateClassAsAdminAsync(adminId, org.Id, year.Id, "2B", UniqueCode("2B"));

        await EnrollStudentAsync(adminId, org.Id, enrolled.Id, studentId);
        await EnrollStudentAsync(adminId, org.Id, other.Id, otherStudentId);

        using var classRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}",
            studentId,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(classRequest)).StatusCode);

        using var enrollmentsRequest = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{org.Id}/classes/{other.Id}/enrollments",
            studentId,
            TestJwt.StudentRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(enrollmentsRequest)).StatusCode);
    }

    private async Task<OrganisationResponse> CreateOrganisationAsAdminAsync(string adminId, string name, string code)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/organisations", adminId, TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateOrganisationRequest(name, code));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OrganisationResponse>();
        return payload ?? throw new InvalidOperationException("Missing organisation payload.");
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
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateYearLevelRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<YearLevelResponse>();
        return payload ?? throw new InvalidOperationException("Missing year level payload.");
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
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new CreateClassRequest(name, code, yearLevelId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ClassResponse>();
        return payload ?? throw new InvalidOperationException("Missing class payload.");
    }

    private async Task AssignTeacherAsync(string adminId, Guid organisationId, Guid classId, string teacherId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/teachers",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new AssignTeacherRequest(teacherId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task EnrollStudentAsync(string adminId, Guid organisationId, Guid classId, string studentId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/organisations/{organisationId}/classes/{classId}/enrollments",
            adminId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new EnrollStudentRequest(studentId));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role)
    {
        using var request = TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<T>();
        return payload ?? throw new InvalidOperationException("Missing response payload.");
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
