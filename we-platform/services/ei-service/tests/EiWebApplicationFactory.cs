using EiService.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EiService.Tests;

public sealed class EiWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeClassAccessChecker AccessChecker { get; } = new();
    public FakeClassInsightsProvider InsightsProvider { get; } = new();
    public FakeAiGatewayClient AiGateway { get; } = new();
    public FakeStudentEiDataClient StudentEiData { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IClassAccessChecker>();
            services.AddSingleton<IClassAccessChecker>(AccessChecker);

            services.RemoveAll<IClassInsightsProvider>();
            services.AddSingleton<IClassInsightsProvider>(InsightsProvider);

            services.RemoveAll<IAiGatewayClient>();
            services.AddSingleton<IAiGatewayClient>(AiGateway);

            services.RemoveAll<IStudentEiDataClient>();
            services.AddSingleton<IStudentEiDataClient>(StudentEiData);
        });
    }
}
