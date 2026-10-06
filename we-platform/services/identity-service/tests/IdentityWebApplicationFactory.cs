using IdentityService.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityService.Tests;

public sealed class IdentityWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IdentityService_{Guid.NewGuid()}";

    public FakeParentClassTeacherSource ParentTeachers { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("InMemoryDatabaseName", _databaseName);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IParentClassTeacherSource>();
            services.AddSingleton<IParentClassTeacherSource>(ParentTeachers);
        });
    }
}

public sealed class FakeParentClassTeacherSource : IParentClassTeacherSource
{
    public IReadOnlyList<string> TeacherUserIds { get; set; } = [];

    public Task<IReadOnlyList<string>> ListTeacherUserIdsAsync(
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TeacherUserIds);
}
