using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudentLearningService.Application;

namespace StudentLearningService.Tests;

public sealed class StudentLearningWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeOrganisationAccessChecker AccessChecker { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrganisationAccessChecker>();
            services.AddSingleton<IOrganisationAccessChecker>(AccessChecker);
        });
    }

    public IProfileEvidenceProcessor GetProcessor() =>
        Services.GetRequiredService<IProfileEvidenceProcessor>();
}
