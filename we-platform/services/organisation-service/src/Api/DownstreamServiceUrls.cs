using Microsoft.Extensions.Configuration;

namespace OrganisationService.Api;

public static class DownstreamServiceUrls
{
    public static void RequireConfigured(IConfiguration configuration)
    {
        Require(configuration, "Ei:BaseUrl");
        Require(configuration, "Intervention:BaseUrl");
    }

    private static void Require(IConfiguration configuration, string key)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
        {
            throw new InvalidOperationException($"{key} is not configured.");
        }
    }
}
