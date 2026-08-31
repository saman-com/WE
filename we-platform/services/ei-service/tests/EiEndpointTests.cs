using System.Net;
using System.Net.Http.Json;
using EiService.Application;

namespace EiService.Tests;

public class EiEndpointTests : IClassFixture<EiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;
    private readonly FakeClassInsightsProvider _insightsProvider;

    public EiEndpointTests(EiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _insightsProvider = factory.InsightsProvider;
    }

    [Fact]
    public async Task Teacher_CanViewClassInsightsForAssignedClass()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();

        _accessChecker.AllowTeacher(teacherId, organisationId, classId);
        _insightsProvider.SetResponse(
            organisationId,
            classId,
            new ClassEiInsightsResponse(
                organisationId,
                classId,
                [
                    new ClassMicroSkillMasteryDistribution(
                        microSkillId,
                        new Dictionary<string, int>
                        {
                            ["Mastered"] = 1,
                            ["Developing"] = 1,
                            ["Proficient"] = 0,
                            ["NotStarted"] = 0
                        },
                        2,
                        "student-1: Strong mastery.",
                        [evidenceId])
                ],
                [
                    new ClassActiveLearningGap(
                        Guid.CreateVersion7(),
                        microSkillId,
                        "High",
                        "High",
                        "student-2",
                        "Expected mastery: Mastered; demonstrated Struggling.",
                        evidenceId)
                ],
                [
                    new ClassDiagnosticTrend(
                        microSkillId,
                        "Struggling",
                        1,
                        DateTimeOffset.UtcNow,
                        "Recent diagnostic trend.",
                        evidenceId)
                ],
                [
                    new StudentNeedingAttention(
                        "student-2",
                        "High severity learning gap",
                        "Expected mastery: Mastered; demonstrated Struggling.",
                        evidenceId)
                ]));

        var response = await SendAsAsync<ClassEiInsightsResponse>(
            HttpMethod.Get,
            $"/api/v1/ei/organisations/{organisationId}/classes/{classId}/insights",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);

        Assert.Equal(classId, response.ClassId);
        Assert.Single(response.MasteryDistribution);
        Assert.Single(response.ActiveLearningGaps);
        Assert.Equal("student-2", response.ActiveLearningGaps[0].StudentUserId);
        Assert.Equal(evidenceId, response.ActiveLearningGaps[0].EvidenceId);
        Assert.Single(response.StudentsNeedingAttention);
    }

    [Fact]
    public async Task Teacher_CannotViewClassInsightsForUnassignedClass()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/ei/organisations/{organisationId}/classes/{classId}/insights",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotViewClassInsightsEndpoint()
    {
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.CreateVersion7();
        var classId = Guid.CreateVersion7();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/ei/organisations/{organisationId}/classes/{classId}/insights",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(
        HttpMethod method,
        string url,
        string userId,
        Guid tenantId,
        params string[] roles)
    {
        using var request = TestJwt.Authorized(method, url, userId, tenantId, roles);
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
