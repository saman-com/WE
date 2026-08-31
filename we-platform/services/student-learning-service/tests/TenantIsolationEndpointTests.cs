using System.Net;
using System.Net.Http.Json;
using StudentLearningService.Application;
using WePlatform.Tenancy;

namespace StudentLearningService.Tests;

public class TenantIsolationEndpointTests : IClassFixture<StudentLearningWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public TenantIsolationEndpointTests(StudentLearningWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task Teacher_FromDifferentTenant_CannotViewStudentProfile()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var adminId = Guid.NewGuid().ToString();
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var classId = Guid.NewGuid();
        _accessChecker.Allow(teacherId, studentId);

        await SyncEnrollmentAsync(adminId, studentId, tenantB, classId, tenantB);

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/students/{studentId}/profile",
            teacherId,
            tenantA,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task SyncEnrollmentAsync(
        string adminId,
        string studentId,
        Guid organisationId,
        Guid classId,
        Guid tenantId)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/students/{studentId}/profile/enrollments",
            adminId,
            tenantId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new SyncProfileEnrollmentRequest(
            organisationId,
            classId,
            "7A",
            "7A"));

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
