namespace IdentityService.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, int ExpiresInSeconds);

public sealed record UserProfileResponse(
    string Id,
    string Email,
    string Name,
    IReadOnlyList<string> Roles);

public sealed record ApiErrorResponse(string Code);
