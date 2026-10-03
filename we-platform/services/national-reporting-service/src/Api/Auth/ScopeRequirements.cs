using Microsoft.AspNetCore.Authorization;
using NationalReportingService.Domain;

namespace NationalReportingService.Api.Auth;

public sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}

public sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        var hasScope = context.User.Claims.Any(claim =>
            claim.Type == ApiKeyAuthenticationHandler.ScopeClaimType
            && string.Equals(claim.Value, requirement.Scope, StringComparison.Ordinal));

        if (hasScope)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class NationalAuthorizationPolicies
{
    public const string Enrollment = "Scope:national:enrollment";
    public const string Mastery = "Scope:national:mastery";
    public const string Coverage = "Scope:national:coverage";

    public static void AddNationalScopePolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(
            Enrollment,
            policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme);
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(NationalApiScopes.Enrollment));
            });

        options.AddPolicy(
            Mastery,
            policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme);
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(NationalApiScopes.Mastery));
            });

        options.AddPolicy(
            Coverage,
            policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme);
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(NationalApiScopes.Coverage));
            });
    }
}
