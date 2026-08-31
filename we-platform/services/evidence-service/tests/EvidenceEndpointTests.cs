using System.Net;
using System.Net.Http.Json;
using EvidenceService.Application;

using WePlatform.Tenancy;

namespace EvidenceService.Tests;

public class EvidenceEndpointTests : IClassFixture<EvidenceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;
    private readonly FakeStudentLearningProfileClient _profileClient;

    public EvidenceEndpointTests(EvidenceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _profileClient = factory.ProfileClient;
    }

    [Fact]
    public async Task Teacher_CanApproveSubmissionWithMarksPerMicroSkill()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assessmentId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var evidence = await ApproveAsync(
            teacherId,
            organisationId,
            classId,
            assessmentId,
            submissionId,
            studentId,
            "Quiz 1 evidence",
            microSkillId,
            4,
            "Clear understanding of reactants.");

        Assert.Equal(assessmentId, evidence.AssessmentId);
        Assert.Equal(submissionId, evidence.SubmissionId);
        Assert.Equal(studentId, evidence.StudentUserId);
        Assert.Equal("Quiz 1 evidence", evidence.Title);
        Assert.Equal("Approved", evidence.Status);
        Assert.Equal(teacherId, evidence.ApprovedByTeacherUserId);
        Assert.Single(evidence.MicroSkillMarks);
        Assert.Equal(microSkillId, evidence.MicroSkillMarks[0].MicroSkillId);
        Assert.Equal(4, evidence.MicroSkillMarks[0].Mark);
        Assert.Equal("Clear understanding of reactants.", evidence.MicroSkillMarks[0].Feedback);
    }

    [Fact]
    public async Task Approval_CreatesEvidenceLinkedToSlpAssessmentAndMicroSkills()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assessmentId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var evidence = await ApproveAsync(
            teacherId,
            organisationId,
            classId,
            assessmentId,
            submissionId,
            studentId,
            "Linked evidence",
            microSkillId,
            3,
            "Developing.");

        var recorded = Assert.Single(_profileClient.Recorded, item => item.EvidenceId == evidence.Id);
        Assert.Equal(studentId, recorded.StudentUserId);
        Assert.Equal(assessmentId, recorded.AssessmentId);
        Assert.Equal(microSkillId, recorded.MicroSkillIds[0]);
        Assert.Equal("Linked evidence", recorded.Title);
    }

    [Fact]
    public async Task ApprovedEvidence_PutReturnsForbidden()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var evidence = await ApproveAsync(
            teacherId,
            organisationId,
            classId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            studentId,
            "Immutable evidence",
            microSkillId,
            5,
            "Excellent.");

        using var request = TestJwt.Authorized(
            HttpMethod.Put,
            $"/api/v1/evidence/{evidence.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new ApproveEvidenceRequest(
            organisationId,
            classId,
            evidence.AssessmentId,
            evidence.SubmissionId,
            studentId,
            "Changed title",
            [new MicroSkillMarkRequest(microSkillId, 1, "Changed.")]));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApprovedEvidence_DeleteReturnsForbidden()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var evidence = await ApproveAsync(
            teacherId,
            organisationId,
            classId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            studentId,
            "Cannot delete",
            Guid.CreateVersion7(),
            2,
            "Needs support.");

        using var request = TestJwt.Authorized(
            HttpMethod.Delete,
            $"/api/v1/evidence/{evidence.Id}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotApproveEvidence()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/evidence",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        request.Content = JsonContent.Create(new ApproveEvidenceRequest(
            organisationId,
            classId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            studentId,
            "Student self-approval",
            [new MicroSkillMarkRequest(Guid.CreateVersion7(), 5, "I did well.")]));

        var recordedBefore = _profileClient.Recorded.Count;
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(recordedBefore, _profileClient.Recorded.Count);
    }

    [Fact]
    public async Task Ai_CannotApproveEvidence()
    {
        var teacherId = Guid.NewGuid().ToString();
        var aiId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/evidence",
            aiId,
            organisationId,
            TestJwt.AiRole);
        request.Content = JsonContent.Create(new ApproveEvidenceRequest(
            organisationId,
            classId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.NewGuid().ToString(),
            "AI approval",
            [new MicroSkillMarkRequest(Guid.CreateVersion7(), 5, "Generated mark.")]));

        var recordedBefore = _profileClient.Recorded.Count;
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(recordedBefore, _profileClient.Recorded.Count);
    }

    [Fact]
    public async Task Teacher_CanViewClassEvidenceSummary()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assessmentId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        await ApproveAsync(
            teacherId,
            organisationId,
            classId,
            assessmentId,
            Guid.CreateVersion7(),
            studentId,
            "Quiz evidence",
            Guid.CreateVersion7(),
            4,
            "Good work.");

        var summaries = await SendAsAsync<List<ClassEvidenceSummaryResponse>>(
            HttpMethod.Get,
            $"/api/v1/evidence/class-summary?organisationId={organisationId}&classId={classId}",
            teacherId,
            TestJwt.TeacherRole,
            organisationId);

        Assert.Contains(summaries, item => item.AssessmentId == assessmentId && item.ReviewedCount == 1);
    }

    [Fact]
    public async Task Teacher_CannotViewClassEvidenceSummaryForUnassignedClass()
    {
        var teacherId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            $"/api/v1/evidence/class-summary?organisationId={organisationId}&classId={classId}",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewOwnApprovedFeedback()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        var otherStudentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var assessmentId = Guid.CreateVersion7();
        var microSkillId = Guid.CreateVersion7();
        _accessChecker.AllowTeacher(teacherId, organisationId, classId);

        var evidence = await ApproveAsync(
            teacherId,
            organisationId,
            classId,
            assessmentId,
            Guid.CreateVersion7(),
            studentId,
            "My approved work",
            microSkillId,
            4,
            "Strong reasoning.");

        await ApproveAsync(
            teacherId,
            organisationId,
            classId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            otherStudentId,
            "Other student work",
            microSkillId,
            3,
            "Private feedback.");

        var feedback = await SendAsAsync<List<StudentFeedbackResponse>>(
            HttpMethod.Get,
            "/api/v1/evidence/student-feedback",
            studentId,
            TestJwt.StudentRole,
            organisationId);

        Assert.Single(feedback);
        Assert.Equal(evidence.Id, feedback[0].Id);
        Assert.Equal("Strong reasoning.", feedback[0].MicroSkillMarks[0].Feedback);
    }

    [Fact]
    public async Task Student_CannotViewStudentFeedbackEndpointAsTeacher()
    {
        var teacherId = Guid.NewGuid().ToString();

        using var request = TestJwt.Authorized(
            HttpMethod.Get,
            "/api/v1/evidence/student-feedback",
            teacherId,
            TestJwt.TeacherRole);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<T> SendAsAsync<T>(HttpMethod method, string url, string userId, string role, Guid? tenantId = null)
    {
        using var request = tenantId.HasValue
            ? TestJwt.Authorized(method, url, userId, tenantId.Value, role)
            : TestJwt.Authorized(method, url, userId, role);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Missing response payload.");
    }

    private async Task<EvidenceResponse> ApproveAsync(
        string teacherId,
        Guid organisationId,
        Guid classId,
        Guid assessmentId,
        Guid submissionId,
        string studentId,
        string title,
        Guid microSkillId,
        decimal mark,
        string feedback)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/evidence",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new ApproveEvidenceRequest(
            organisationId,
            classId,
            assessmentId,
            submissionId,
            studentId,
            title,
            [new MicroSkillMarkRequest(microSkillId, mark, feedback)]));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceResponse>()
            ?? throw new InvalidOperationException("Missing evidence payload.");
    }
}
