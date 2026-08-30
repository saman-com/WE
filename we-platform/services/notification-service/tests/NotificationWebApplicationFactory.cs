using NotificationService.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NotificationService.Tests;

public sealed class NotificationWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeEmailNotifier EmailNotifier { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailNotifier>();
            services.AddSingleton<IEmailNotifier>(EmailNotifier);
        });
    }
}
