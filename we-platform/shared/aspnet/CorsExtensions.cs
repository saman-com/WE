using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WePlatform.AspNetCore;

public static class CorsExtensions
{
    public const string PolicyName = "WebPortal";
    public const string AllowedOriginsKey = "Cors:AllowedOrigins";
    public const string DevelopmentDefaultOrigin = "http://localhost:3000";

    public static string[] ResolveAllowedOrigins(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetSection(AllowedOriginsKey).Get<string[]>()
            ?? Array.Empty<string>();
        var origins = configured
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length > 0)
        {
            return origins;
        }

        return environment.IsDevelopment()
            ? [DevelopmentDefaultOrigin]
            : [];
    }

    public static IServiceCollection AddWePlatformCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var origins = ResolveAllowedOrigins(configuration, environment);

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    // No browser origins configured outside Development — deny all.
                    policy.SetIsOriginAllowed(_ => false);
                }
                else
                {
                    policy.WithOrigins(origins);
                }

                policy.AllowAnyHeader().AllowAnyMethod();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseWePlatformCors(this IApplicationBuilder app) =>
        app.UseCors(PolicyName);
}
