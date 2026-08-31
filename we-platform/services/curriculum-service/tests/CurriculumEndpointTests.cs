using System.Net;
using System.Net.Http.Json;
using CurriculumService.Application;

using WePlatform.Tenancy;

namespace CurriculumService.Tests;

public class CurriculumEndpointTests : IClassFixture<CurriculumWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CurriculumEndpointTests(CurriculumWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/curriculum");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotCreateCurriculum()
    {
        var studentId = Guid.NewGuid().ToString();
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/curriculum",
            studentId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new CreateCurriculumRequest(
            Guid.NewGuid(),
            "Denied Curriculum",
            "2026",
            "Draft"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotListCurriculum()
    {
        var studentId = Guid.NewGuid().ToString();
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum?organisationId={Guid.NewGuid()}",
            studentId,
            TestJwt.StudentRole);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CanCrudCurriculumSubjectAndUnit()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();

        var created = await CreateCurriculumAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            "Cambridge Science",
            "2027");
        Assert.Equal("Cambridge Science", created.Name);
        Assert.Equal("2027", created.Version);
        Assert.Equal(organisationId, created.OrganisationId);
        Assert.Equal("Draft", created.Status);

        var listed = await SendAsAsync<List<CurriculumResponse>>(
            HttpMethod.Get,
            $"/api/v1/curriculum?organisationId={organisationId}",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);
        Assert.Contains(listed, item => item.Id == created.Id);

        var fetched = await SendAsAsync<CurriculumResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{created.Id}",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);
        Assert.Equal(created.Id, fetched.Id);

        using var updateCurriculum = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/curriculum/{created.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        updateCurriculum.Content = JsonContent.Create(
            new UpdateCurriculumRequest("Cambridge Science", "2027.1", "Active"));
        var updateCurriculumResponse = await _client.SendAsync(updateCurriculum);
        Assert.Equal(HttpStatusCode.OK, updateCurriculumResponse.StatusCode);
        var updatedCurriculum = await updateCurriculumResponse.Content.ReadFromJsonAsync<CurriculumResponse>();
        Assert.Equal("Active", updatedCurriculum!.Status);
        Assert.Equal("2027.1", updatedCurriculum.Version);

        var subject = await CreateSubjectAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            created.Id,
            "Chemistry",
            "CHEM",
            1);
        Assert.Equal("Chemistry", subject.Name);
        Assert.Equal(created.Id, subject.CurriculumId);

        using var updateSubject = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/curriculum/{created.Id}/subjects/{subject.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        updateSubject.Content = JsonContent.Create(new UpdateSubjectRequest("Chemistry", "CHM", 2));
        var updateSubjectResponse = await _client.SendAsync(updateSubject);
        Assert.Equal(HttpStatusCode.OK, updateSubjectResponse.StatusCode);

        var unit = await CreateUnitAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            created.Id,
            subject.Id,
            "Chemical Reactions",
            1);
        Assert.Equal("Chemical Reactions", unit.Name);
        Assert.Equal(subject.Id, unit.SubjectId);
        Assert.Equal(created.Id, unit.CurriculumId);

        using var updateUnit = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/curriculum/{created.Id}/subjects/{subject.Id}/units/{unit.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        updateUnit.Content = JsonContent.Create(new UpdateUnitRequest("Chemical Changes", 2));
        var updateUnitResponse = await _client.SendAsync(updateUnit);
        Assert.Equal(HttpStatusCode.OK, updateUnitResponse.StatusCode);

        var topic = await CreateTopicAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            created.Id,
            subject.Id,
            unit.Id,
            "Balancing Equations",
            1);
        Assert.Equal("Balancing Equations", topic.Name);
        Assert.Equal(unit.Id, topic.UnitId);

        using var deleteTopic = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{created.Id}/subjects/{subject.Id}/units/{unit.Id}/topics/{topic.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteTopic)).StatusCode);

        using var deleteUnit = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{created.Id}/subjects/{subject.Id}/units/{unit.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteUnit)).StatusCode);

        using var deleteSubject = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{created.Id}/subjects/{subject.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteSubject)).StatusCode);

        using var deleteCurriculum = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{created.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteCurriculum)).StatusCode);

        using var missing = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{created.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missing)).StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateCurriculumLinkedToOrganisation()
    {
        var adminId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();

        var created = await CreateCurriculumAsync(
            adminId,
            TestJwt.AdminRole,
            organisationId,
            "National Mathematics",
            "2026");

        Assert.Equal(organisationId, created.OrganisationId);

        var otherOrganisationId = Guid.NewGuid();
        var other = await CreateCurriculumAsync(
            adminId,
            TestJwt.AdminRole,
            otherOrganisationId,
            "Other School Science",
            "2026");

        var listed = await SendAsAsync<List<CurriculumResponse>>(
            HttpMethod.Get,
            $"/api/v1/curriculum?organisationId={organisationId}",
            adminId,
            TestJwt.AdminRole,
            organisationId);

        Assert.Contains(listed, item => item.Id == created.Id);
        Assert.DoesNotContain(listed, item => item.Id == other.Id);
        Assert.All(listed, item => Assert.Equal(organisationId, item.OrganisationId));
    }

    [Fact]
    public async Task CreateCurriculum_WithoutOrganisation_ReturnsBadRequest()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/curriculum",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateCurriculumRequest(
            Guid.Empty,
            "Unscoped",
            "2026",
            "Draft"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_ForUnknownSubject_ReturnsNotFound()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            "Orphan Guard",
            "2026");

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{Guid.NewGuid()}/units",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateUnitRequest("Orphan Unit", 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUnit_ForSubjectInDifferentCurriculum_ReturnsNotFound()
    {
        var teacherId = Guid.NewGuid().ToString();
        var firstOrganisationId = Guid.NewGuid();
        var secondOrganisationId = Guid.NewGuid();
        var first = await CreateCurriculumAsync(
            teacherId,
            TestJwt.TeacherRole,
            firstOrganisationId,
            "Curriculum A",
            "2026");
        var second = await CreateCurriculumAsync(
            teacherId,
            TestJwt.TeacherRole,
            secondOrganisationId,
            "Curriculum B",
            "2026");
        var subject = await CreateSubjectAsync(
            teacherId,
            TestJwt.TeacherRole,
            firstOrganisationId,
            first.Id,
            "Physics",
            "PHY",
            1);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{second.Id}/subjects/{subject.Id}/units",
            teacherId,
            secondOrganisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateUnitRequest("Mis-scoped Unit", 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletingSubject_RemovesUnits_LeavingNoOrphans()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            "Integrity Curriculum",
            "2026");
        var subject = await CreateSubjectAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            curriculum.Id,
            "Biology",
            "BIO",
            1);
        var unit = await CreateUnitAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            curriculum.Id,
            subject.Id,
            "Cells",
            1);

        using var deleteSubject = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteSubject)).StatusCode);

        using var missingUnit = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missingUnit)).StatusCode);

        using var missingSubjectUnits = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missingSubjectUnits)).StatusCode);

        var tree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/tree",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);
        Assert.DoesNotContain(tree.Subjects, item => item.Id == subject.Id);
        Assert.DoesNotContain(
            tree.Subjects.SelectMany(item => item.Units),
            item => item.Id == unit.Id);
    }

    [Fact]
    public async Task Tree_ReturnsNavigableSubjectUnitHierarchy()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            "Science Tree",
            "2026");
        var chemistry = await CreateSubjectAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            curriculum.Id,
            "Chemistry",
            "CHEM",
            1);
        var physics = await CreateSubjectAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            curriculum.Id,
            "Physics",
            "PHY",
            2);
        var reactions = await CreateUnitAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            curriculum.Id,
            chemistry.Id,
            "Chemical Reactions",
            1);
        await CreateUnitAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            curriculum.Id,
            physics.Id,
            "Forces",
            1);
        await CreateTopicAsync(
            teacherId,
            TestJwt.TeacherRole,
            organisationId,
            curriculum.Id,
            chemistry.Id,
            reactions.Id,
            "Physical Changes",
            1);

        var tree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/tree",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

        Assert.Equal(curriculum.Id, tree.Id);
        Assert.Equal(organisationId, tree.OrganisationId);
        Assert.Equal(2, tree.Subjects.Count);
        var chemistryNode = Assert.Single(tree.Subjects, subject => subject.Id == chemistry.Id);
        var unitNode = Assert.Single(chemistryNode.Units);
        Assert.Equal(reactions.Id, unitNode.Id);
        Assert.Single(unitNode.Topics);
        Assert.Contains(tree.Subjects, subject => subject.Id == physics.Id && subject.Units.Count == 1);
    }

    private async Task<CurriculumResponse> CreateCurriculumAsync(
        string userId,
        string role,
        Guid organisationId,
        string name,
        string version)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/curriculum", userId, organisationId, role);
        request.Content = JsonContent.Create(new CreateCurriculumRequest(organisationId, name, version, "Draft"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurriculumResponse>();
        return payload ?? throw new InvalidOperationException("Missing curriculum payload.");
    }

    private async Task<SubjectResponse> CreateSubjectAsync(
        string userId,
        string role,
        Guid organisationId,
        Guid curriculumId,
        string name,
        string code,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects",
            userId,
            organisationId,
            role);
        request.Content = JsonContent.Create(new CreateSubjectRequest(name, code, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<SubjectResponse>();
        return payload ?? throw new InvalidOperationException("Missing subject payload.");
    }

    private async Task<UnitResponse> CreateUnitAsync(
        string userId,
        string role,
        Guid organisationId,
        Guid curriculumId,
        Guid subjectId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units",
            userId,
            organisationId,
            role);
        request.Content = JsonContent.Create(new CreateUnitRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UnitResponse>();
        return payload ?? throw new InvalidOperationException("Missing unit payload.");
    }

    private async Task<TopicResponse> CreateTopicAsync(
        string userId,
        string role,
        Guid organisationId,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/topics",
            userId,
            organisationId,
            role);
        request.Content = JsonContent.Create(new CreateTopicRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TopicResponse>();
        return payload ?? throw new InvalidOperationException("Missing topic payload.");
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role, Guid? tenantId = null)
    {
        using var request = tenantId.HasValue
            ? TestJwt.Authorized(method, url, userId, tenantId.Value, role)
            : TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<T>();
        return payload ?? throw new InvalidOperationException("Missing response payload.");
    }
}
