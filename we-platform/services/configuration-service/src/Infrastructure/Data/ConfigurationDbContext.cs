using ConfigurationService.Domain;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace ConfigurationService.Infrastructure.Data;

public sealed class ConfigurationDbContext(
    DbContextOptions<ConfigurationDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<RegionalConfiguration> RegionalConfigurations => Set<RegionalConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegionalConfiguration>(entity =>
        {
            entity.ToTable("regional_configurations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.Property(e => e.AcademicCalendarJson).HasColumnName("academic_calendar_json").IsRequired();
            entity.Property(e => e.GradingScaleJson).HasColumnName("grading_scale_json").IsRequired();
            entity.Property(e => e.AssessmentModelsJson).HasColumnName("assessment_models_json").IsRequired();
            entity.Property(e => e.ReportingTemplatesJson).HasColumnName("reporting_templates_json").IsRequired();
            entity.Property(e => e.LocaleSettingsJson).HasColumnName("locale_settings_json").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
