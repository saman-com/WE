using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace NationalReportingService.Tests;

public sealed class NationalReportingWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"NationalReportingService-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:DatabaseName"] = _databaseName
            });
        });
    }
}
