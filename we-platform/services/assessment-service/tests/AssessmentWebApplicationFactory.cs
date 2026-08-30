using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AssessmentService.Application;

namespace AssessmentService.Tests;

public sealed class AssessmentWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeClassAccessChecker AccessChecker { get; } = new();
    public FakeParentAccessChecker ParentAccessChecker { get; } = new();
    public FakeAiGatewayClient AiGateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClassAccessChecker>();
            services.AddSingleton<IClassAccessChecker>(AccessChecker);
            services.RemoveAll<IParentAccessChecker>();
            services.AddSingleton<IParentAccessChecker>(ParentAccessChecker);
            services.RemoveAll<IAiGatewayClient>();
            services.AddSingleton<IAiGatewayClient>(AiGateway);
        });
    }
}
