using InterventionService.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InterventionService.Tests;

public sealed class InterventionWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"InterventionService_{Guid.NewGuid()}";

    public FakeOrganisationAccessChecker AccessChecker { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("InMemoryDatabaseName", _databaseName);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrganisationAccessChecker>();
            services.AddSingleton<IOrganisationAccessChecker>(AccessChecker);
        });
    }
}
