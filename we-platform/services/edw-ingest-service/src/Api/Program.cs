using EdwIngestService.Infrastructure;
using EdwIngestService.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEdwIngestInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EdwDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
