using WePlatform.Tenancy;

namespace ConfigurationService.Domain;

public sealed class RegionalConfiguration : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string AcademicCalendarJson { get; set; } = DefaultJson.EmptyObject;
    public string GradingScaleJson { get; set; } = DefaultJson.EmptyObject;
    public string AssessmentModelsJson { get; set; } = DefaultJson.EmptyArray;
    public string ReportingTemplatesJson { get; set; } = DefaultJson.EmptyArray;
    public string LocaleSettingsJson { get; set; } = DefaultJson.EmptyObject;
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedByUserId { get; set; } = string.Empty;
}

internal static class DefaultJson
{
    public const string EmptyObject = "{}";
    public const string EmptyArray = "[]";
}
