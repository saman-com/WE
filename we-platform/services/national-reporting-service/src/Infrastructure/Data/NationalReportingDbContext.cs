using Microsoft.EntityFrameworkCore;
using NationalReportingService.Domain;

namespace NationalReportingService.Infrastructure.Data;

public sealed class NationalReportingDbContext(DbContextOptions<NationalReportingDbContext> options)
    : DbContext(options)
{
    public DbSet<MinistryApiKey> MinistryApiKeys => Set<MinistryApiKey>();
    public DbSet<RegionalEnrollmentFact> RegionalEnrollmentFacts => Set<RegionalEnrollmentFact>();
    public DbSet<RegionalMasteryFact> RegionalMasteryFacts => Set<RegionalMasteryFact>();
    public DbSet<RegionalCoverageFact> RegionalCoverageFacts => Set<RegionalCoverageFact>();
    public DbSet<RegionalEquityFact> RegionalEquityFacts => Set<RegionalEquityFact>();
    public DbSet<RegionalCurriculumEffectivenessFact> RegionalCurriculumEffectivenessFacts =>
        Set<RegionalCurriculumEffectivenessFact>();
    public DbSet<RegionalInterventionImpactFact> RegionalInterventionImpactFacts =>
        Set<RegionalInterventionImpactFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MinistryApiKey>(entity =>
        {
            entity.ToTable("ministry_api_keys");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientName).HasColumnName("client_name").IsRequired();
            entity.Property(e => e.KeyHash).HasColumnName("key_hash").IsRequired();
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.Property(e => e.Scopes).HasColumnName("scopes").IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<RegionalEnrollmentFact>(entity =>
        {
            entity.ToTable("regional_enrollment_facts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RegionCode).HasColumnName("region_code").IsRequired();
            entity.Property(e => e.SchoolCode).HasColumnName("school_code").IsRequired();
            entity.HasIndex(e => new { e.RegionCode, e.SchoolCode, e.AsOfDate }).IsUnique();
            entity.Property(e => e.StudentCount).HasColumnName("student_count");
            entity.Property(e => e.TeacherCount).HasColumnName("teacher_count");
            entity.Property(e => e.AsOfDate).HasColumnName("as_of_date");
        });

        modelBuilder.Entity<RegionalMasteryFact>(entity =>
        {
            entity.ToTable("regional_mastery_facts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RegionCode).HasColumnName("region_code").IsRequired();
            entity.Property(e => e.SubjectCode).HasColumnName("subject_code").IsRequired();
            entity.HasIndex(e => new { e.RegionCode, e.SubjectCode, e.AsOfDate }).IsUnique();
            entity.Property(e => e.AverageMasteryPercent).HasColumnName("average_mastery_percent");
            entity.Property(e => e.MasteredSharePercent).HasColumnName("mastered_share_percent");
            entity.Property(e => e.SampleSize).HasColumnName("sample_size");
            entity.Property(e => e.AsOfDate).HasColumnName("as_of_date");
        });

        modelBuilder.Entity<RegionalCoverageFact>(entity =>
        {
            entity.ToTable("regional_coverage_facts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RegionCode).HasColumnName("region_code").IsRequired();
            entity.Property(e => e.CurriculumCode).HasColumnName("curriculum_code").IsRequired();
            entity.HasIndex(e => new { e.RegionCode, e.CurriculumCode, e.AsOfDate }).IsUnique();
            entity.Property(e => e.CoveredObjectivePercent).HasColumnName("covered_objective_percent");
            entity.Property(e => e.SchoolsReporting).HasColumnName("schools_reporting");
            entity.Property(e => e.AsOfDate).HasColumnName("as_of_date");
        });

        modelBuilder.Entity<RegionalEquityFact>(entity =>
        {
            entity.ToTable("regional_equity_facts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RegionCode).HasColumnName("region_code").IsRequired();
            entity.Property(e => e.DemographicDimension).HasColumnName("demographic_dimension").IsRequired();
            entity.Property(e => e.DemographicCategory).HasColumnName("demographic_category").IsRequired();
            entity.HasIndex(e => new
            {
                e.RegionCode,
                e.DemographicDimension,
                e.DemographicCategory,
                e.AsOfDate
            }).IsUnique();
            entity.Property(e => e.AverageMasteryPercent).HasColumnName("average_mastery_percent");
            entity.Property(e => e.SampleSize).HasColumnName("sample_size");
            entity.Property(e => e.AsOfDate).HasColumnName("as_of_date");
        });

        modelBuilder.Entity<RegionalCurriculumEffectivenessFact>(entity =>
        {
            entity.ToTable("regional_curriculum_effectiveness_facts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RegionCode).HasColumnName("region_code").IsRequired();
            entity.Property(e => e.CurriculumCode).HasColumnName("curriculum_code").IsRequired();
            entity.Property(e => e.SubjectCode).HasColumnName("subject_code").IsRequired();
            entity.HasIndex(e => new
            {
                e.RegionCode,
                e.CurriculumCode,
                e.SubjectCode,
                e.AsOfDate
            }).IsUnique();
            entity.Property(e => e.MasteryRatePercent).HasColumnName("mastery_rate_percent");
            entity.Property(e => e.CoveragePercent).HasColumnName("coverage_percent");
            entity.Property(e => e.SchoolsReporting).HasColumnName("schools_reporting");
            entity.Property(e => e.AsOfDate).HasColumnName("as_of_date");
        });

        modelBuilder.Entity<RegionalInterventionImpactFact>(entity =>
        {
            entity.ToTable("regional_intervention_impact_facts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RegionCode).HasColumnName("region_code").IsRequired();
            entity.Property(e => e.InterventionType).HasColumnName("intervention_type").IsRequired();
            entity.HasIndex(e => new { e.RegionCode, e.InterventionType, e.AsOfDate }).IsUnique();
            entity.Property(e => e.TotalCount).HasColumnName("total_count");
            entity.Property(e => e.SuccessfulCount).HasColumnName("successful_count");
            entity.Property(e => e.AverageGrowthPercent).HasColumnName("average_growth_percent");
            entity.Property(e => e.AsOfDate).HasColumnName("as_of_date");
        });

        base.OnModelCreating(modelBuilder);
    }
}
