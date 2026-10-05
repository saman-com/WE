using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace WePlatform.AspNetCore;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public const string ContentTypeOptions = "nosniff";
    public const string FrameOptions = "DENY";
    public const string ReferrerPolicy = "strict-origin-when-cross-origin";
    public const string HstsValue = "max-age=31536000; includeSubDomains";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = ContentTypeOptions;
            headers["X-Frame-Options"] = FrameOptions;
            headers["Referrer-Policy"] = ReferrerPolicy;

            if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            {
                headers["Strict-Transport-Security"] = HstsValue;
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseWePlatformSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
