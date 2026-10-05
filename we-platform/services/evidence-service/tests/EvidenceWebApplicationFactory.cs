using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EvidenceService.Application;

namespace EvidenceService.Tests;

public sealed class EvidenceWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeClassAccessChecker AccessChecker { get; } = new();
    public FakeParentAccessChecker ParentAccessChecker { get; } = new();
    public FakeDomainEventPublisher EventPublisher { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClassAccessChecker>();
            services.AddSingleton<IClassAccessChecker>(AccessChecker);
            services.RemoveAll<IParentAccessChecker>();
            services.AddSingleton<IParentAccessChecker>(ParentAccessChecker);
            services.RemoveAll<IDomainEventPublisher>();
            services.AddSingleton<IDomainEventPublisher>(EventPublisher);
        });
    }
}
