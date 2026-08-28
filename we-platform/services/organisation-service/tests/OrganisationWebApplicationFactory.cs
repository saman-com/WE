using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrganisationService.Application;

namespace OrganisationService.Tests;

public sealed class OrganisationWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeStudentLearningProfileClient ProfileClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStudentLearningProfileClient>();
            services.AddSingleton<IStudentLearningProfileClient>(ProfileClient);
        });
    }
}
