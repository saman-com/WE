using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EvidenceService.Application;
using EvidenceService.Infrastructure.Data;
using EvidenceService.Infrastructure.Organisation;
using EvidenceService.Infrastructure.StudentLearning;

namespace EvidenceService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEvidenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EvidenceDb");

        services.AddDbContext<EvidenceDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("EvidenceService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IClassAccessChecker, HttpClassAccessChecker>();
        services.AddHttpClient<IStudentLearningProfileClient, HttpStudentLearningProfileClient>();
        return services;
    }
}
