extern alias evidence;
extern alias learning;
extern alias diagnostic;
extern alias gaps;
extern alias mastery;

namespace EvidenceApprovalFlow.Tests;

internal sealed class AllowAllClassAccess : evidence::EvidenceService.Application.IClassAccessChecker
{
    public Task<bool> TeacherCanManageClassAsync(
        string teacherUserId,
        Guid organisationId,
        Guid classId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

internal sealed class AllowAllLearningOrgAccess : learning::StudentLearningService.Application.IOrganisationAccessChecker
{
    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

internal sealed class AllowAllDiagnosticOrgAccess : diagnostic::DiagnosticService.Application.IOrganisationAccessChecker
{
    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

internal sealed class AllowAllGapOrgAccess : gaps::LearningGapService.Application.IOrganisationAccessChecker
{
    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

internal sealed class AllowAllMasteryOrgAccess : mastery::MasteryService.Application.IOrganisationAccessChecker
{
    public Task<bool> TeacherCanViewStudentAsync(
        string teacherUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
