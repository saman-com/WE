using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrganisationService.Application;
using OrganisationService.Infrastructure.Assessment;
using OrganisationService.Infrastructure.Data;
using OrganisationService.Infrastructure.Evidence;
using OrganisationService.Infrastructure.Intervention;
using OrganisationService.Infrastructure.Mastery;
using OrganisationService.Infrastructure.Ei;
using OrganisationService.Infrastructure.StudentLearning;

namespace OrganisationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganisationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrganisationDb");

        services.AddDbContext<OrganisationDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("OrganisationService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddHttpClient<IStudentLearningProfileClient, HttpStudentLearningProfileClient>();
        services.AddHttpClient<IAssessmentDashboardClient, HttpAssessmentDashboardClient>();
        services.AddHttpClient<IEvidenceDashboardClient, HttpEvidenceDashboardClient>();
        services.AddHttpClient<IMasteryDashboardClient, HttpMasteryDashboardClient>();
        services.AddHttpClient<IInterventionDashboardClient, HttpInterventionDashboardClient>();
        services.AddHttpClient<IEiInsightsClient, HttpEiInsightsClient>();

        return services;
    }
}
