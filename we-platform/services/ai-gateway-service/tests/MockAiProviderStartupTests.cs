using AiGatewayService.Infrastructure;
using AiGatewayService.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AiGatewayService.Tests;

public class MockAiProviderStartupTests
{
    [Fact]
    public void EnsureMockAllowed_InProduction_WithoutOverride_Throws()
    {
        var environment = new FakeHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiProvider:Provider"] = "Mock",
                ["AiProvider:AllowMockInProduction"] = "false"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MockAiProviderStartup.EnsureMockAllowedInEnvironment(environment, configuration));

        Assert.Contains("AllowMockInProduction", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureMockAllowed_InProduction_WithOverride_DoesNotThrow()
    {
        var environment = new FakeHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiProvider:AllowMockInProduction"] = "true"
            })
            .Build();

        MockAiProviderStartup.EnsureMockAllowedInEnvironment(environment, configuration);
    }

    [Fact]
    public void EnsureMockAllowed_InDevelopment_DoesNotThrow()
    {
        var environment = new FakeHostEnvironment(Environments.Development);
        var configuration = new ConfigurationBuilder().Build();

        MockAiProviderStartup.EnsureMockAllowedInEnvironment(environment, configuration);
    }

    [Fact]
    public void AddAiGatewayInfrastructure_InProductionWithoutOverride_Throws()
    {
        var services = new ServiceCollection();
        var environment = new FakeHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiProvider:Provider"] = "Mock",
                ["AiProvider:AllowMockInProduction"] = "false",
                ["ConnectionStrings:AiGatewayDb"] = ""
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddAiGatewayInfrastructure(configuration, environment));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("Mock", true)]
    [InlineData("mock", true)]
    [InlineData("OpenAI", false)]
    public void IsMockProvider_MatchesExpected(string? provider, bool expected) =>
        Assert.Equal(expected, MockAiProviderStartup.IsMockProvider(provider));

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "AiGatewayService.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
