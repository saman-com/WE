using System.Net.Http.Json;
using WePlatform.Events;

namespace EvidenceService.Tests;

public class DomainEventPublishingTests : IClassFixture<EvidenceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeClassAccessChecker _accessChecker;
    private readonly FakeDomainEventPublisher _eventPublisher;

    public DomainEventPublishingTests(EvidenceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
        _eventPublisher = factory.EventPublisher;
    }

    [Fact]
    public async Task Approval_PublishesEvidenceCreatedEvent()
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
            "Event evidence",
            microSkillId,
            4,
            "Well done.");

        var published = _eventPublisher.EvidenceCreatedEvents.Single(e => e.EvidenceId == evidence.Id);
        Assert.Equal(evidence.Id, published.EvidenceId);
        Assert.Equal(assessmentId, published.AssessmentId);
        Assert.Equal(submissionId, published.SubmissionId);
        Assert.Equal(studentId, published.StudentUserId);
        Assert.Equal(classId, published.ClassId);
        Assert.Equal(organisationId, published.OrganisationId);
        Assert.Equal(teacherId, published.ApprovedByTeacherUserId);
        Assert.Equal(EvidenceCreated.CurrentVersion, published.Version);
        Assert.NotEqual(Guid.Empty, published.EventId);
        Assert.NotEqual(Guid.Empty, published.CorrelationId);
        Assert.Single(published.MicroSkillMarks);
        Assert.Equal(microSkillId, published.MicroSkillMarks[0].MicroSkillId);
        Assert.Equal(4, published.MicroSkillMarks[0].Mark);
        Assert.Equal("Well done.", published.MicroSkillMarks[0].Feedback);
    }

    [Fact]
    public async Task Approval_PublishesAssessmentApprovedEventWithMicroSkillResults()
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
            "Assessment approval evidence",
            microSkillId,
            3,
            "Developing understanding.");

        var published = _eventPublisher.AssessmentApprovedEvents.Single(e => e.EvidenceId == evidence.Id);
        Assert.Equal(assessmentId, published.AssessmentId);
        Assert.Equal(studentId, published.StudentUserId);
        Assert.Equal(submissionId, published.SubmissionId);
        Assert.Equal(evidence.Id, published.EvidenceId);
        Assert.Equal(teacherId, published.ApprovedByTeacherUserId);
        Assert.Equal(organisationId, published.OrganisationId);
        Assert.Equal(AssessmentApproved.CurrentVersion, published.Version);
        Assert.NotEqual(Guid.Empty, published.EventId);
        Assert.NotEqual(Guid.Empty, published.CorrelationId);
        Assert.Single(published.MicroSkillResults);
        Assert.Equal(microSkillId, published.MicroSkillResults[0].MicroSkillId);
        Assert.Equal(3, published.MicroSkillResults[0].Mark);
        Assert.Equal("Developing understanding.", published.MicroSkillResults[0].Feedback);
    }

    [Fact]
    public async Task FailedApproval_DoesNotPublishEvents()
    {
        var studentId = Guid.NewGuid().ToString();
        var organisationId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var evidenceCreatedBefore = _eventPublisher.EvidenceCreatedEvents.Count;
        var assessmentApprovedBefore = _eventPublisher.AssessmentApprovedEvents.Count;

        using var request = TestJwt.Authorized(
            System.Net.Http.HttpMethod.Post,
            "/api/v1/evidence",
            studentId,
            organisationId,
            TestJwt.StudentRole);
        request.Content = System.Net.Http.Json.JsonContent.Create(new Application.ApproveEvidenceRequest(
            organisationId,
            classId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            studentId,
            "Student self-approval",
            [new Application.MicroSkillMarkRequest(Guid.CreateVersion7(), 5, "I did well.")]));

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(evidenceCreatedBefore, _eventPublisher.EvidenceCreatedEvents.Count);
        Assert.Equal(assessmentApprovedBefore, _eventPublisher.AssessmentApprovedEvents.Count);
    }

    private async Task<Application.EvidenceResponse> ApproveAsync(
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
            System.Net.Http.HttpMethod.Post,
            "/api/v1/evidence",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = System.Net.Http.Json.JsonContent.Create(new Application.ApproveEvidenceRequest(
            organisationId,
            classId,
            assessmentId,
            submissionId,
            studentId,
            title,
            [new Application.MicroSkillMarkRequest(microSkillId, mark, feedback)]));

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Application.EvidenceResponse>()
            ?? throw new InvalidOperationException("Missing evidence payload.");
    }
}
