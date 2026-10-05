using NationalReportingService.Domain;

namespace NationalReportingService.Tests;

internal static class TestApiKey
{
    public const string FullAccessKey = "test-ministry-full-access-key";
    public const string EnrollmentOnlyKey = "test-ministry-enrollment-only-key";
    public const string InactiveKey = "test-ministry-inactive-key";
    public const string RateLimitKey = "test-ministry-rate-limit-key";
    public const string SuppressionEnrollmentKey = "test-ministry-suppression-enrollment-key";
    public const string SuppressionMasteryKey = "test-ministry-suppression-mastery-key";
    public const string SuppressionCoverageKey = "test-ministry-suppression-coverage-key";

    public static HttpRequestMessage Authorized(HttpMethod method, string url, string apiKey)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Api-Key", apiKey);
        return request;
    }

    public static string FullScopes => string.Join(',', NationalApiScopes.All);
}
