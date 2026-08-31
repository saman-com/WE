using System.Security.Claims;
using System.Text.Json;
using ConfigurationService.Application;
using ConfigurationService.Domain;
using ConfigurationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace ConfigurationService.Api;

public static class ConfigurationEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapConfigurationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/regional-configuration").RequireAuthorization();

        api.MapGet("/", GetRegionalConfiguration);
        api.MapPut("/", UpdateRegionalConfiguration);
    }

    private static async Task<IResult> GetRegionalConfiguration(
        ClaimsPrincipal principal,
        ConfigurationDbContext db,
        ITenantContext tenantContext)
    {
        if (!tenantContext.HasTenant)
        {
            return Results.Forbid();
        }

        var configuration = await db.RegionalConfigurations
            .FirstOrDefaultAsync(item => item.TenantId == tenantContext.TenantId);

        if (configuration is null)
        {
            return Results.Ok(CreateDefaultResponse(tenantContext.TenantId!.Value));
        }

        return Results.Ok(ToResponse(configuration));
    }

    private static async Task<IResult> UpdateRegionalConfiguration(
        UpdateRegionalConfigurationRequest request,
        ClaimsPrincipal principal,
        ConfigurationDbContext db,
        ITenantContext tenantContext)
    {
        if (!principal.CanManageRegionalConfiguration())
        {
            return Results.Forbid();
        }

        if (!tenantContext.HasTenant || !IsValidRequest(request))
        {
            return Results.BadRequest();
        }

        var existing = await db.RegionalConfigurations
            .FirstOrDefaultAsync(item => item.TenantId == tenantContext.TenantId);

        if (existing is null)
        {
            existing = new RegionalConfiguration
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantContext.TenantId!.Value
            };
            db.RegionalConfigurations.Add(existing);
        }

        existing.AcademicCalendarJson = JsonSerializer.Serialize(request.AcademicCalendar, JsonOptions);
        existing.GradingScaleJson = JsonSerializer.Serialize(request.GradingScale, JsonOptions);
        existing.AssessmentModelsJson = JsonSerializer.Serialize(request.AssessmentModels, JsonOptions);
        existing.ReportingTemplatesJson = JsonSerializer.Serialize(request.ReportingTemplates, JsonOptions);
        existing.LocaleSettingsJson = JsonSerializer.Serialize(request.LocaleSettings, JsonOptions);
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.UpdatedByUserId = principal.UserId();

        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(existing));
    }

    private static bool IsValidRequest(UpdateRegionalConfigurationRequest request)
    {
        if (request.AcademicCalendar is null
            || request.GradingScale is null
            || string.IsNullOrWhiteSpace(request.GradingScale.Name)
            || request.AssessmentModels is null
            || request.ReportingTemplates is null
            || request.LocaleSettings is null
            || string.IsNullOrWhiteSpace(request.LocaleSettings.LanguageCode)
            || string.IsNullOrWhiteSpace(request.LocaleSettings.TimeZone))
        {
            return false;
        }

        return true;
    }

    private static RegionalConfigurationResponse CreateDefaultResponse(Guid tenantId) =>
        new(
            tenantId,
            new AcademicCalendarDto([], []),
            new GradingScaleDto("Default", []),
            [],
            [],
            new LocaleSettingsDto("en", "US", "yyyy-MM-dd", "UTC"),
            DateTimeOffset.UnixEpoch,
            string.Empty);

    private static RegionalConfigurationResponse ToResponse(RegionalConfiguration configuration) =>
        new(
            configuration.TenantId,
            DeserializeOrDefault(configuration.AcademicCalendarJson, new AcademicCalendarDto([], [])),
            DeserializeOrDefault(configuration.GradingScaleJson, new GradingScaleDto("Default", [])),
            DeserializeOrDefault(configuration.AssessmentModelsJson, Array.Empty<AssessmentModelDto>()),
            DeserializeOrDefault(configuration.ReportingTemplatesJson, Array.Empty<ReportingTemplateDto>()),
            DeserializeOrDefault(
                configuration.LocaleSettingsJson,
                new LocaleSettingsDto("en", "US", "yyyy-MM-dd", "UTC")),
            configuration.UpdatedAt,
            configuration.UpdatedByUserId);

    private static T DeserializeOrDefault<T>(string json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
    }

    private static bool CanManageRegionalConfiguration(this ClaimsPrincipal principal) =>
        principal.IsInRole(PlatformRoles.SchoolLeader)
        || principal.IsInRole(PlatformRoles.SystemAdministrator);

    private static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue(ClaimTypes.Name)
        ?? string.Empty;
}
