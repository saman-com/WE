using System.Net;
using System.Net.Http.Json;
using InterventionService.Application;

namespace InterventionService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<InterventionWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(InterventionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task CrossSchool_InterventionList_BothDirections_NeverReturnsOtherSchoolData()
    {
        // Probe: intervention-service:intervention-list
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherA, studentA);
        _accessChecker.AllowTeacher(teacherB, studentB);

        var interventionA = await CreateInterventionAsync(teacherA, schoolA, studentA, "School A intervention");
        var interventionB = await CreateInterventionAsync(teacherB, schoolB, studentB, "School B intervention");

        using var listA = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/interventions?studentUserId={studentA}",
            teacherA,
            schoolA,
            TestJwt.TeacherRole);
        var okA = await _client.SendAsync(listA);
        okA.EnsureSuccessStatusCode();
        var itemsA = await okA.Content.ReadFromJsonAsync<StudentInterventionsResponse>();
        Assert.Contains(itemsA!.Interventions, item => item.Id == interventionA.Id);
        Assert.DoesNotContain(itemsA.Interventions, item => item.Id == interventionB.Id);

        using var listB = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/interventions?studentUserId={studentB}",
            teacherB,
            schoolB,
            TestJwt.TeacherRole);
        var okB = await _client.SendAsync(listB);
        okB.EnsureSuccessStatusCode();
        var itemsB = await okB.Content.ReadFromJsonAsync<StudentInterventionsResponse>();
        Assert.Contains(itemsB!.Interventions, item => item.Id == interventionB.Id);
        Assert.DoesNotContain(itemsB.Interventions, item => item.Id == interventionA.Id);

        using var createIntoA = TestJwt.Authorized(HttpMethod.Post, "/api/v1/interventions", teacherB, schoolB, TestJwt.TeacherRole);
        createIntoA.Content = JsonContent.Create(new CreateInterventionRequest(
            schoolA,
            studentA,
            Guid.CreateVersion7(),
            "Cross-school create",
            null,
            null,
            null,
            null));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(createIntoA)).StatusCode);
    }

    [Fact]
    public async Task CrossSchool_InterventionResource_BothDirections_Denied()
    {
        // Probe: intervention-service:intervention-resource
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var teacherA = Guid.NewGuid().ToString();
        var teacherB = Guid.NewGuid().ToString();
        var studentA = Guid.NewGuid().ToString();
        var studentB = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherA, studentA);
        _accessChecker.AllowTeacher(teacherB, studentB);

        var interventionA = await CreateInterventionAsync(teacherA, schoolA, studentA, "Resource A");
        var interventionB = await CreateInterventionAsync(teacherB, schoolB, studentB, "Resource B");

        await AssertDeniedAsync(HttpMethod.Get, interventionA.Id, teacherB, schoolB);
        await AssertDeniedAsync(HttpMethod.Get, interventionB.Id, teacherA, schoolA);
        await AssertDeniedAsync(HttpMethod.Patch, interventionA.Id, teacherB, schoolB);
        await AssertDeniedAsync(HttpMethod.Patch, interventionB.Id, teacherA, schoolA);
    }

    [Fact]
    public async Task CrossSchool_OrganisationPath_BothDirections_Denied()
    {
        // Probe: intervention-service:organisation-path
        var schoolA = Guid.CreateVersion7();
        var schoolB = Guid.CreateVersion7();
        var leaderA = Guid.NewGuid().ToString();
        var leaderB = Guid.NewGuid().ToString();
        _accessChecker.AllowSchoolLeaderForOrganisation(leaderA, schoolA);
        _accessChecker.AllowSchoolLeaderForOrganisation(leaderB, schoolB);

        await AssertOrgListDeniedAsync(schoolA, leaderB, schoolB);
        await AssertOrgListDeniedAsync(schoolB, leaderA, schoolA);
    }

    private async Task AssertOrgListDeniedAsync(Guid targetOrganisationId, string callerId, Guid callerTenant)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/organisations/{targetOrganisationId}/interventions",
            callerId,
            callerTenant,
            TestJwt.SchoolLeaderRole);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task AssertDeniedAsync(HttpMethod method, Guid interventionId, string callerId, Guid callerTenant)
    {
        using var request = TestJwt.Authorized(
            method,
            $"/api/v1/interventions/{interventionId}",
            callerId,
            callerTenant,
            TestJwt.TeacherRole);
        if (method == HttpMethod.Patch)
        {
            request.Content = JsonContent.Create(new PatchInterventionRequest(null, null, null, null, null, null, null));
        }

        var response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"{method} intervention returned {response.StatusCode}");
    }

    private async Task<InterventionResponse> CreateInterventionAsync(
        string teacherId,
        Guid organisationId,
        string studentId,
        string plannedActions)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateInterventionRequest(
            organisationId,
            studentId,
            Guid.CreateVersion7(),
            plannedActions,
            null,
            null,
            null,
            null));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InterventionResponse>())!;
    }
}
