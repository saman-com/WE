using WePlatform.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWePlatformSecurityHeaders();

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
