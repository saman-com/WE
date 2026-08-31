using System.Net;
using System.Net.Http.Json;
using CurriculumService.Application;

using WePlatform.Tenancy;

namespace CurriculumService.Tests;

public class LearningObjectiveEndpointTests : IClassFixture<CurriculumWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LearningObjectiveEndpointTests(CurriculumWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Teacher_CanCrudLearningObjectiveAndMicroSkill()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(
            teacherId,
            organisationId,
            "Science Objectives",
            "2026");
        var subject = await CreateSubjectAsync(teacherId, organisationId, curriculum.Id, "Chemistry", "CHEM", 1);
        var unit = await CreateUnitAsync(teacherId, organisationId, curriculum.Id, subject.Id, "Chemical Reactions", 1);

        var objective = await CreateLearningObjectiveAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            "Balance chemical equations",
            1);
        Assert.Equal("Balance chemical equations", objective.Title);
        Assert.Equal(unit.Id, objective.UnitId);
        Assert.NotEqual(Guid.Empty, objective.Id);

        var listed = await SendAsAsync<List<LearningObjectiveResponse>>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives",
            teacherId,
            organisationId);
        Assert.Contains(listed, item => item.Id == objective.Id);

        using var updateObjective = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        updateObjective.Content = JsonContent.Create(
            new UpdateLearningObjectiveRequest("Balance equations accurately", 2));
        var updateObjectiveResponse = await _client.SendAsync(updateObjective);
        Assert.Equal(HttpStatusCode.OK, updateObjectiveResponse.StatusCode);

        var microSkill = await CreateMicroSkillAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            objective.Id,
            "Identify reactants and products",
            1);
        Assert.Equal("Identify reactants and products", microSkill.Name);
        Assert.Equal(objective.Id, microSkill.LearningObjectiveId);
        Assert.NotEqual(Guid.Empty, microSkill.Id);

        var microSkills = await SendAsAsync<List<MicroSkillResponse>>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}/micro-skills",
            teacherId,
            organisationId);
        Assert.Contains(microSkills, item => item.Id == microSkill.Id);

        using var updateMicroSkill = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}/micro-skills/{microSkill.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        updateMicroSkill.Content = JsonContent.Create(
            new UpdateMicroSkillRequest("Identify reactants and products in an equation", 2));
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(updateMicroSkill)).StatusCode);

        using var deleteMicroSkill = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}/micro-skills/{microSkill.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteMicroSkill)).StatusCode);

        using var deleteObjective = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteObjective)).StatusCode);
    }

    [Fact]
    public async Task CreateLearningObjective_ForUnknownUnit_ReturnsNotFound()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(teacherId, organisationId, "Guard", "2026");
        var subject = await CreateSubjectAsync(teacherId, organisationId, curriculum.Id, "Physics", "PHY", 1);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{Guid.NewGuid()}/learning-objectives",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateLearningObjectiveRequest("Orphan objective", 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateMicroSkill_ForUnknownLearningObjective_ReturnsNotFound()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(teacherId, organisationId, "Guard", "2026");
        var subject = await CreateSubjectAsync(teacherId, organisationId, curriculum.Id, "Biology", "BIO", 1);
        var unit = await CreateUnitAsync(teacherId, organisationId, curriculum.Id, subject.Id, "Cells", 1);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{Guid.NewGuid()}/micro-skills",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateMicroSkillRequest("Orphan skill", 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletingUnit_CascadesLearningObjectivesAndMicroSkills()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(teacherId, organisationId, "Cascade", "2026");
        var subject = await CreateSubjectAsync(teacherId, organisationId, curriculum.Id, "Math", "MATH", 1);
        var unit = await CreateUnitAsync(teacherId, organisationId, curriculum.Id, subject.Id, "Algebra", 1);
        var objective = await CreateLearningObjectiveAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            "Solve linear equations",
            1);
        var microSkill = await CreateMicroSkillAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            objective.Id,
            "Isolate the variable",
            1);

        using var deleteUnit = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteUnit)).StatusCode);

        using var missingObjective = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missingObjective)).StatusCode);

        using var missingMicroSkill = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}/micro-skills/{microSkill.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missingMicroSkill)).StatusCode);

        var tree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/tree",
            teacherId,
            organisationId);
        Assert.DoesNotContain(
            tree.Subjects.SelectMany(subjectNode => subjectNode.Units),
            item => item.Id == unit.Id);
    }

    [Fact]
    public async Task DeletingLearningObjective_CascadesMicroSkills()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(teacherId, organisationId, "Objective Cascade", "2026");
        var subject = await CreateSubjectAsync(teacherId, organisationId, curriculum.Id, "History", "HIST", 1);
        var unit = await CreateUnitAsync(teacherId, organisationId, curriculum.Id, subject.Id, "World War II", 1);
        var objective = await CreateLearningObjectiveAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            "Explain causes of the war",
            1);
        var microSkill = await CreateMicroSkillAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            objective.Id,
            "Identify key treaties",
            1);

        using var deleteObjective = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(deleteObjective)).StatusCode);

        using var missingMicroSkill = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{objective.Id}/micro-skills/{microSkill.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(missingMicroSkill)).StatusCode);
    }

    [Fact]
    public async Task Tree_ReturnsLearningObjectiveMicroSkillHierarchyUnderUnit()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var curriculum = await CreateCurriculumAsync(teacherId, organisationId, "Hierarchy", "2026");
        var subject = await CreateSubjectAsync(teacherId, organisationId, curriculum.Id, "English", "ENG", 1);
        var unit = await CreateUnitAsync(teacherId, organisationId, curriculum.Id, subject.Id, "Poetry", 1);
        var objective = await CreateLearningObjectiveAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            "Analyse poetic devices",
            1);
        var microSkill = await CreateMicroSkillAsync(
            teacherId,
            organisationId,
            curriculum.Id,
            subject.Id,
            unit.Id,
            objective.Id,
            "Identify metaphor",
            1);

        var tree = await SendAsAsync<CurriculumTreeResponse>(
            HttpMethod.Get,
            $"/api/v1/curriculum/{curriculum.Id}/tree",
            teacherId,
            organisationId);

        var unitNode = Assert.Single(
            tree.Subjects.SelectMany(subjectNode => subjectNode.Units),
            item => item.Id == unit.Id);
        var objectiveNode = Assert.Single(unitNode.LearningObjectives);
        Assert.Equal(objective.Id, objectiveNode.Id);
        Assert.Equal("Analyse poetic devices", objectiveNode.Title);
        var microSkillNode = Assert.Single(objectiveNode.MicroSkills);
        Assert.Equal(microSkill.Id, microSkillNode.Id);
        Assert.Equal("Identify metaphor", microSkillNode.Name);
    }

    private async Task<CurriculumResponse> CreateCurriculumAsync(
        string userId,
        Guid organisationId,
        string name,
        string version)
    {
        using var request = TestJwt.Authorized(HttpMethod.Post, "/api/v1/curriculum", userId, organisationId, TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateCurriculumRequest(organisationId, name, version, "Draft"));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CurriculumResponse>())
            ?? throw new InvalidOperationException("Missing curriculum payload.");
    }

    private async Task<SubjectResponse> CreateSubjectAsync(
        string userId,
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
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateSubjectRequest(name, code, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SubjectResponse>())
            ?? throw new InvalidOperationException("Missing subject payload.");
    }

    private async Task<UnitResponse> CreateUnitAsync(
        string userId,
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
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateUnitRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UnitResponse>())
            ?? throw new InvalidOperationException("Missing unit payload.");
    }

    private async Task<LearningObjectiveResponse> CreateLearningObjectiveAsync(
        string userId,
        Guid organisationId,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        string title,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/learning-objectives",
            userId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateLearningObjectiveRequest(title, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LearningObjectiveResponse>())
            ?? throw new InvalidOperationException("Missing learning objective payload.");
    }

    private async Task<MicroSkillResponse> CreateMicroSkillAsync(
        string userId,
        Guid organisationId,
        Guid curriculumId,
        Guid subjectId,
        Guid unitId,
        Guid learningObjectiveId,
        string name,
        int sortOrder)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculumId}/subjects/{subjectId}/units/{unitId}/learning-objectives/{learningObjectiveId}/micro-skills",
            userId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateMicroSkillRequest(name, sortOrder));
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MicroSkillResponse>())
            ?? throw new InvalidOperationException("Missing micro-skill payload.");
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, Guid? tenantId = null)
    {
        using var request = tenantId.HasValue
            ? TestJwt.Authorized(method, url, userId, tenantId.Value, TestJwt.TeacherRole)
            : TestJwt.Authorized(method, url, userId, TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())
            ?? throw new InvalidOperationException("Missing response payload.");
    }
}
