using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace WePlatform.AspNetCore.Tests;

public class CorsExtensionsTests
{
    [Fact]
    public void ResolveAllowedOrigins_InDevelopment_WithEmptyConfig_ReturnsLocalhostDefault()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new FakeHostEnvironment(Environments.Development);

        var origins = CorsExtensions.ResolveAllowedOrigins(configuration, environment);

        Assert.Equal([CorsExtensions.DevelopmentDefaultOrigin], origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_WithConfiguredOrigins_ReturnsConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://portal.example.com",
                ["Cors:AllowedOrigins:1"] = " https://admin.example.com ",
                ["Cors:AllowedOrigins:2"] = "https://portal.example.com",
                ["Cors:AllowedOrigins:3"] = ""
            })
            .Build();
        var environment = new FakeHostEnvironment(Environments.Production);

        var origins = CorsExtensions.ResolveAllowedOrigins(configuration, environment);

        Assert.Equal(
            ["https://portal.example.com", "https://admin.example.com"],
            origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_InProduction_WithEmptyConfig_ReturnsEmpty()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = ""
            })
            .Build();
        var environment = new FakeHostEnvironment(Environments.Production);

        var origins = CorsExtensions.ResolveAllowedOrigins(configuration, environment);

        Assert.Empty(origins);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "WePlatform.AspNetCore.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
