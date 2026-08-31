using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityService.Tests;

public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IdentityService_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("InMemoryDatabaseName", _databaseName);
    }
}
