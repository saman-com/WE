using DiagnosticService.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiagnosticService.Tests;

public sealed class DiagnosticWebApplicationFactory : WebApplicationFactory<Program>
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

    public IDiagnosticProcessor GetProcessor() =>
        Services.GetRequiredService<IDiagnosticProcessor>();
}
