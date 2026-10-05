using System.Net;
using System.Net.Http.Json;
using CurriculumService.Application;

namespace CurriculumService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<CurriculumWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantIsolationEndpointTests(CurriculumWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CrossSchool_CurriculumResource_BothDirections_Denied_ForAllHierarchyRoutes()
    {
        // Probe: curriculum-service:curriculum-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();

        var treeA = await SeedTreeAsync(teacherA, schoolA);
        var treeB = await SeedTreeAsync(teacherB, schoolB);

        foreach (var (method, path) in HierarchyRoutes(treeA))
        {
            await AssertDeniedAsync(method, path, teacherB, schoolB);
        }

        foreach (var (method, path) in HierarchyRoutes(treeB))
        {
            await AssertDeniedAsync(method, path, teacherA, schoolA);
        }

        await AssertDeniedAsync(
            "POST",
            $"/api/v1/curriculum/{treeA.CurriculumId}/inherit",
            teacherB,
            schoolB);
        await AssertDeniedAsync(
            "POST",
            $"/api/v1/curriculum/{treeB.CurriculumId}/inherit",
            teacherA,
            schoolA);
    }

    [Fact]
    public async Task CrossSchool_CurriculumList_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: curriculum-service:curriculum-list
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();

        var treeA = await SeedTreeAsync(teacherA, schoolA);
        var treeB = await SeedTreeAsync(teacherB, schoolB);

        using var listAsB = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum?organisationId={schoolA}",
            teacherB,
            schoolB,
            TestJwt.TeacherRole);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(listAsB)).StatusCode);

        using var listAsA = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/curriculum?organisationId={schoolA}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(listAsA);
        response.EnsureSuccessStatusCode();
        var listed = await response.Content.ReadFromJsonAsync<List<CurriculumResponse>>();
        Assert.Contains(listed!, item => item.Id == treeA.CurriculumId);
        Assert.DoesNotContain(listed!, item => item.Id == treeB.CurriculumId);

        using var createIntoA = TestJwt.Authorized(HttpMethod.Post, "/api/v1/curriculum", teacherB, schoolB, TestJwt.TeacherRole);
        createIntoA.Content = JsonContent.Create(new CreateCurriculumRequest(schoolA, "Leak", "2026", null));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(createIntoA)).StatusCode);
    }

    private async Task AssertDeniedAsync(string method, string path, string userId, Guid tenantId)
    {
        using var request = TestJwt.Authorized(new HttpMethod(method), path, userId, tenantId, TestJwt.TeacherRole);
        if (method is "PUT" or "POST")
        {
            request.Content = JsonContent.Create(new { name = "x", sortOrder = 1, code = "X", description = "x" });
        }

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden
                or HttpStatusCode.NotFound
                or HttpStatusCode.BadRequest,
            $"{method} {path} returned {response.StatusCode}");
    }

    private static IEnumerable<(string Method, string Path)> HierarchyRoutes(SeededTree tree)
    {
        var c = tree.CurriculumId;
        var s = tree.SubjectId;
        var u = tree.UnitId;
        var t = tree.TopicId;
        var lo = tree.LearningObjectiveId;
        var ms = tree.MicroSkillId;

        yield return ("GET", $"/api/v1/curriculum/{c}");
        yield return ("PUT", $"/api/v1/curriculum/{c}");
        yield return ("DELETE", $"/api/v1/curriculum/{c}");
        yield return ("GET", $"/api/v1/curriculum/{c}/tree");
        yield return ("GET", $"/api/v1/curriculum/{c}/variant-links");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects");
        yield return ("POST", $"/api/v1/curriculum/{c}/subjects");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}");
        yield return ("PUT", $"/api/v1/curriculum/{c}/subjects/{s}");
        yield return ("DELETE", $"/api/v1/curriculum/{c}/subjects/{s}");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units");
        yield return ("POST", $"/api/v1/curriculum/{c}/subjects/{s}/units");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}");
        yield return ("PUT", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}");
        yield return ("DELETE", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/topics");
        yield return ("POST", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/topics");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/topics/{t}");
        yield return ("PUT", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/topics/{t}");
        yield return ("DELETE", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/topics/{t}");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives");
        yield return ("POST", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}");
        yield return ("PUT", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}");
        yield return ("DELETE", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}/micro-skills");
        yield return ("POST", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}/micro-skills");
        yield return ("GET", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}/micro-skills/{ms}");
        yield return ("PUT", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}/micro-skills/{ms}");
        yield return ("DELETE", $"/api/v1/curriculum/{c}/subjects/{s}/units/{u}/learning-objectives/{lo}/micro-skills/{ms}");
    }

    private async Task<SeededTree> SeedTreeAsync(string teacherId, Guid organisationId)
    {
        using var createCurriculum = TestJwt.Authorized(HttpMethod.Post, "/api/v1/curriculum", teacherId, organisationId, TestJwt.TeacherRole);
        createCurriculum.Content = JsonContent.Create(new CreateCurriculumRequest(organisationId, "Isolation Curriculum", "2026", null));
        var curriculumResponse = await _client.SendAsync(createCurriculum);
        curriculumResponse.EnsureSuccessStatusCode();
        var curriculum = (await curriculumResponse.Content.ReadFromJsonAsync<CurriculumResponse>())!;

        using var createSubject = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        createSubject.Content = JsonContent.Create(new CreateSubjectRequest("Science", "SCI", 1));
        var subjectResponse = await _client.SendAsync(createSubject);
        subjectResponse.EnsureSuccessStatusCode();
        var subject = (await subjectResponse.Content.ReadFromJsonAsync<SubjectResponse>())!;

        using var createUnit = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        createUnit.Content = JsonContent.Create(new CreateUnitRequest("Forces", 1));
        var unitResponse = await _client.SendAsync(createUnit);
        unitResponse.EnsureSuccessStatusCode();
        var unit = (await unitResponse.Content.ReadFromJsonAsync<UnitResponse>())!;

        using var createTopic = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/topics",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        createTopic.Content = JsonContent.Create(new CreateTopicRequest("Motion", 1));
        var topicResponse = await _client.SendAsync(createTopic);
        topicResponse.EnsureSuccessStatusCode();
        var topic = (await topicResponse.Content.ReadFromJsonAsync<TopicResponse>())!;

        using var createLo = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        createLo.Content = JsonContent.Create(new CreateLearningObjectiveRequest("Describe forces", 1));
        var loResponse = await _client.SendAsync(createLo);
        loResponse.EnsureSuccessStatusCode();
        var lo = (await loResponse.Content.ReadFromJsonAsync<LearningObjectiveResponse>())!;

        using var createMs = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/curriculum/{curriculum.Id}/subjects/{subject.Id}/units/{unit.Id}/learning-objectives/{lo.Id}/micro-skills",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        createMs.Content = JsonContent.Create(new CreateMicroSkillRequest("Identify force pairs", 1));
        var msResponse = await _client.SendAsync(createMs);
        msResponse.EnsureSuccessStatusCode();
        var ms = (await msResponse.Content.ReadFromJsonAsync<MicroSkillResponse>())!;

        return new SeededTree(curriculum.Id, subject.Id, unit.Id, topic.Id, lo.Id, ms.Id);
    }

    private sealed record SeededTree(
        Guid CurriculumId,
        Guid SubjectId,
        Guid UnitId,
        Guid TopicId,
        Guid LearningObjectiveId,
        Guid MicroSkillId);
}
