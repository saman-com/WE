namespace ConfigurationService.Application;

public record AcademicTermDto(string Name, DateOnly StartDate, DateOnly EndDate);

public record HolidayDto(string Name, DateOnly Date);

public record AcademicCalendarDto(
    IReadOnlyList<AcademicTermDto> Terms,
    IReadOnlyList<HolidayDto> Holidays);

public record GradingLevelDto(string Label, decimal MinScore, decimal MaxScore);

public record GradingScaleDto(
    string Name,
    IReadOnlyList<GradingLevelDto> Levels);

public record AssessmentModelDto(string Name, string Category);

public record ReportingTemplateDto(string Id, string Name, string Format);

public record LocaleSettingsDto(
    string LanguageCode,
    string RegionCode,
    string DateFormat,
    string TimeZone);

public record RegionalConfigurationResponse(
    Guid TenantId,
    AcademicCalendarDto AcademicCalendar,
    GradingScaleDto GradingScale,
    IReadOnlyList<AssessmentModelDto> AssessmentModels,
    IReadOnlyList<ReportingTemplateDto> ReportingTemplates,
    LocaleSettingsDto LocaleSettings,
    DateTimeOffset UpdatedAt,
    string UpdatedByUserId);

public record UpdateRegionalConfigurationRequest(
    AcademicCalendarDto AcademicCalendar,
    GradingScaleDto GradingScale,
    IReadOnlyList<AssessmentModelDto> AssessmentModels,
    IReadOnlyList<ReportingTemplateDto> ReportingTemplates,
    LocaleSettingsDto LocaleSettings);
