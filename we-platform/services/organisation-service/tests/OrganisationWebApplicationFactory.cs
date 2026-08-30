using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class OrganisationWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeStudentLearningProfileClient ProfileClient { get; } = new();
    public FakeAssessmentDashboardClient AssessmentClient { get; } = new();
    public FakeEvidenceDashboardClient EvidenceClient { get; } = new();
    public FakeMasteryDashboardClient MasteryClient { get; } = new();
    public FakeInterventionDashboardClient InterventionClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStudentLearningProfileClient>();
            services.AddSingleton<IStudentLearningProfileClient>(ProfileClient);
            services.RemoveAll<IAssessmentDashboardClient>();
            services.AddSingleton<IAssessmentDashboardClient>(AssessmentClient);
            services.RemoveAll<IEvidenceDashboardClient>();
            services.AddSingleton<IEvidenceDashboardClient>(EvidenceClient);
            services.RemoveAll<IMasteryDashboardClient>();
            services.AddSingleton<IMasteryDashboardClient>(MasteryClient);
            services.RemoveAll<IInterventionDashboardClient>();
            services.AddSingleton<IInterventionDashboardClient>(InterventionClient);
        });
    }
}
