using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CurriculumService.Infrastructure.Data;
using WePlatform.Tenancy;

namespace CurriculumService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCurriculumInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("CurriculumDb");

        services.AddDbContext<CurriculumDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("CurriculumService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
