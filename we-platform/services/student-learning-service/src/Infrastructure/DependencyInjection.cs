using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StudentLearningService.Application;
using StudentLearningService.Infrastructure.Data;
using StudentLearningService.Infrastructure.Messaging;
using StudentLearningService.Infrastructure.Organisation;
using WePlatform.Tenancy;

namespace StudentLearningService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStudentLearningInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("StudentLearningDb");

        services.AddDbContext<StudentLearningDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("StudentLearningService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IProfileEvidenceProcessor, ProfileEvidenceProcessor>();
        services.AddMemoryCache();
        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        services.AddStudentLearningMessaging(configuration, environment);

        return services;
    }
}
