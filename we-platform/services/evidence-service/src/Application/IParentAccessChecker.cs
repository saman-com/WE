namespace EvidenceService.Application;

public interface IParentAccessChecker
{
    Task<bool> ParentCanViewStudentAsync(
        string parentUserId,
        string studentUserId,
        string bearerToken,
        CancellationToken cancellationToken = default);
}
