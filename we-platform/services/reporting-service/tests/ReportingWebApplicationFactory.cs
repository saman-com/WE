using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReportingService.Application;

namespace ReportingService.Tests;

public sealed class ReportingWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeOrganisationAccessChecker AccessChecker { get; } = new();
    public FakeEiInsightsClient EiInsightsClient { get; } = new();
    public FakeClassDashboardClient ClassDashboardClient { get; } = new();
    public FakeSchoolSummaryClient SchoolSummaryClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrganisationAccessChecker>();
            services.AddSingleton<IOrganisationAccessChecker>(AccessChecker);

            services.RemoveAll<IEiInsightsClient>();
            services.AddSingleton<IEiInsightsClient>(EiInsightsClient);

            services.RemoveAll<IClassDashboardClient>();
            services.AddSingleton<IClassDashboardClient>(ClassDashboardClient);

            services.RemoveAll<ISchoolSummaryClient>();
            services.AddSingleton<ISchoolSummaryClient>(SchoolSummaryClient);
        });
    }
}
