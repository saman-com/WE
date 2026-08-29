using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AssessmentService.Application;
using AssessmentService.Infrastructure.Data;
using AssessmentService.Infrastructure.Organisation;

namespace AssessmentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAssessmentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AssessmentDb");

        services.AddDbContext<AssessmentDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("AssessmentService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IClassAccessChecker, HttpClassAccessChecker>();

        return services;
    }
}
