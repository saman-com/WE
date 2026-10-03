using Microsoft.AspNetCore.Authentication;

namespace NationalReportingService.Api.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}
