using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Email;

namespace NotificationService.Tests;

public class MockEmailStartupTests
{
    [Fact]
    public void EnsureMockAllowed_InProduction_WithoutOverride_Throws()
    {
        var environment = new FakeHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:AllowMockInProduction"] = "false"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MockEmailStartup.EnsureMockAllowedInEnvironment(environment, configuration));

        Assert.Contains("AllowMockInProduction", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureMockAllowed_InProduction_WithOverride_DoesNotThrow()
    {
        var environment = new FakeHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:AllowMockInProduction"] = "true"
            })
            .Build();

        MockEmailStartup.EnsureMockAllowedInEnvironment(environment, configuration);
    }

    [Fact]
    public void EnsureMockAllowed_InDevelopment_DoesNotThrow()
    {
        var environment = new FakeHostEnvironment(Environments.Development);
        var configuration = new ConfigurationBuilder().Build();

        MockEmailStartup.EnsureMockAllowedInEnvironment(environment, configuration);
    }

    [Fact]
    public void AddNotificationInfrastructure_InProductionWithoutOverride_Throws()
    {
        var services = new ServiceCollection();
        var environment = new FakeHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:AllowMockInProduction"] = "false",
                ["ConnectionStrings:NotificationDb"] = ""
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddNotificationInfrastructure(configuration, environment));
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "NotificationService.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
