using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentLearningService.Application;
using StudentLearningService.Infrastructure.Data;
using StudentLearningService.Infrastructure.Organisation;
using WePlatform.Tenancy;

namespace StudentLearningService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddStudentLearningInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
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

        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();

        return services;
    }
}
