namespace IdentityService.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, int ExpiresInSeconds);

public sealed record UserProfileResponse(
    string Id,
    string Email,
    string Name,
    IReadOnlyList<string> Roles);

public sealed record DirectoryUserResponse(
    string Id,
    string Name,
    string Email,
    IReadOnlyList<string> Roles);

public sealed record ParentTeacherResponse(string Id, string Name);

public sealed record ParentChildNameResponse(string Id, string Name);

public interface IParentClassTeacherSource
{
    Task<IReadOnlyList<string>> ListTeacherUserIdsAsync(
        string bearerToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListLinkedStudentUserIdsAsync(
        string bearerToken,
        CancellationToken cancellationToken = default);
}

public sealed record CreateUserRequest(string Email, string Password, string Name, string Role);

public sealed record ApiErrorResponse(string Code);
