using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportingService.Application;
using ReportingService.Domain;
using ReportingService.Infrastructure.Analytics;
using ReportingService.Infrastructure.Data;
using ReportingService.Infrastructure.Data.Edw;
using ReportingService.Infrastructure.Ei;
using ReportingService.Infrastructure.Export;
using ReportingService.Infrastructure.Organisation;
using WePlatform.Tenancy;

namespace ReportingService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddWePlatformTenancy();
        var connectionString = configuration.GetConnectionString("ReportingDb");
        var edwConnectionString = configuration.GetConnectionString("EdwDb");

        services.AddDbContext<ReportingDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("ReportingService");
                return;
            }

            options.UseNpgsql(connectionString);
        });

        services.AddDbContext<EdwAnalyticsDbContext>(options =>
        {
            if (environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(edwConnectionString))
            {
                options.UseInMemoryDatabase("EdwAnalytics");
                return;
            }

            options.UseNpgsql(edwConnectionString);
        });

        services.AddHttpClient<IOrganisationAccessChecker, HttpOrganisationAccessChecker>();
        services.AddMemoryCache();
        services.AddHttpClient<IEiInsightsClient, HttpEiInsightsClient>();
        services.AddHttpClient<IClassDashboardClient, HttpClassDashboardClient>();
        services.AddHttpClient<ISchoolSummaryClient, HttpSchoolSummaryClient>();
        services.AddSingleton<IReportPdfExporter, QuestPdfReportExporter>();
        services.AddScoped<IReportGenerator, ReportGenerator>();
        services.AddScoped<ILongitudinalAnalyticsQuery, LongitudinalAnalyticsQuery>();
        services.AddScoped<IEffectivenessAnalyticsQuery, EffectivenessAnalyticsQuery>();

        return services;
    }
}

internal sealed class ReportGenerator(
    ReportingDbContext db,
    IEiInsightsClient eiInsightsClient,
    IClassDashboardClient classDashboardClient,
    ISchoolSummaryClient schoolSummaryClient,
    IReportPdfExporter pdfExporter,
    ITenantContext tenantContext) : IReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ReportResponse?> GenerateClassProgressReportAsync(
        Guid organisationId,
        Guid classId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var insights = await eiInsightsClient.GetClassInsightsAsync(
            organisationId,
            classId,
            bearerToken,
            cancellationToken);
        var dashboard = await classDashboardClient.GetClassDashboardAsync(
            organisationId,
            classId,
            bearerToken,
            cancellationToken);

        if (insights is null || dashboard is null)
        {
            return null;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var content = ClassProgressReportBuilder.Build(insights, dashboard, generatedAt);
        var report = new GeneratedReport
        {
            Id = Guid.CreateVersion7(),
            TenantId = organisationId,
            ReportType = ReportTypes.ClassProgress,
            OrganisationId = organisationId,
            ClassId = classId,
            RequestedByUserId = requestedByUserId,
            ContentJson = JsonSerializer.Serialize(content, JsonOptions),
            GeneratedAt = generatedAt
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(report, content);
    }

    public async Task<ReportResponse?> GenerateSchoolSummaryReportAsync(
        Guid organisationId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var summary = await schoolSummaryClient.GetSchoolSummaryAsync(
            organisationId,
            bearerToken,
            cancellationToken);

        if (summary is null)
        {
            return null;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var content = SchoolSummaryReportBuilder.Build(summary, generatedAt);
        var report = new GeneratedReport
        {
            Id = Guid.CreateVersion7(),
            TenantId = organisationId,
            ReportType = ReportTypes.SchoolSummary,
            OrganisationId = organisationId,
            ClassId = null,
            RequestedByUserId = requestedByUserId,
            ContentJson = JsonSerializer.Serialize(content, JsonOptions),
            GeneratedAt = generatedAt
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(report, content);
    }

    public async Task<ReportResponse?> GetReportAsync(
        Guid reportId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var report = await db.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken);

        if (report is null || TenantAccess.ValidateEntityAccess(tenantContext, report) is not null)
        {
            return null;
        }

        return ToResponse(report, DeserializeContent(report));
    }

    public async Task<byte[]?> ExportReportPdfAsync(
        Guid reportId,
        string requestedByUserId,
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        var report = await db.Reports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken);

        if (report is null || TenantAccess.ValidateEntityAccess(tenantContext, report) is not null)
        {
            return null;
        }

        return report.ReportType switch
        {
            ReportTypes.ClassProgress => pdfExporter.ExportClassProgressReport(
                JsonSerializer.Deserialize<ClassProgressReportContent>(report.ContentJson, JsonOptions)!),
            ReportTypes.SchoolSummary => pdfExporter.ExportSchoolSummaryReport(
                JsonSerializer.Deserialize<SchoolSummaryReportContent>(report.ContentJson, JsonOptions)!),
            _ => null
        };
    }

    private static object DeserializeContent(GeneratedReport report) =>
        report.ReportType switch
        {
            ReportTypes.ClassProgress => JsonSerializer.Deserialize<ClassProgressReportContent>(
                report.ContentJson,
                JsonOptions)!,
            ReportTypes.SchoolSummary => JsonSerializer.Deserialize<SchoolSummaryReportContent>(
                report.ContentJson,
                JsonOptions)!,
            _ => JsonSerializer.Deserialize<object>(report.ContentJson, JsonOptions)!
        };

    private static ReportResponse ToResponse(GeneratedReport report, object content) =>
        new(
            report.Id,
            report.ReportType,
            report.OrganisationId,
            report.ClassId,
            report.RequestedByUserId,
            report.GeneratedAt,
            content);
}
