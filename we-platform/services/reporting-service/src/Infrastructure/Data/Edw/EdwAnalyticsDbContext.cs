using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace ReportingService.Infrastructure.Data.Edw;

public sealed class EdwAnalyticsDbContext(
    DbContextOptions<EdwAnalyticsDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<EdwDimTime> DimTimes => Set<EdwDimTime>();
    public DbSet<EdwEvidenceFact> EvidenceFacts => Set<EdwEvidenceFact>();
    public DbSet<EdwInterventionFact> InterventionFacts => Set<EdwInterventionFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EdwDimTime>(entity =>
        {
            entity.ToTable("dim_time");
            entity.HasKey(e => e.DateKey);
            entity.ConfigureTenantId();
            entity.Property(e => e.DateKey).HasColumnName("date_key");
            entity.Property(e => e.CalendarDate).HasColumnName("calendar_date");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Day).HasColumnName("day");
        });

        modelBuilder.Entity<EdwEvidenceFact>(entity =>
        {
            entity.ToTable("fact_evidence");
            entity.HasKey(e => e.EventId);
            entity.ConfigureTenantId();
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ApprovedByTeacherUserId).HasColumnName("approved_by_teacher_user_id").IsRequired();
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.TimeKey).HasColumnName("time_key");
            entity.Property(e => e.MicroSkillCount).HasColumnName("micro_skill_count");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.SubjectName).HasColumnName("subject_name");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.UnitName).HasColumnName("unit_name");
            entity.Property(e => e.MasteredMicroSkillCount).HasColumnName("mastered_micro_skill_count");
            entity.Property(e => e.TotalMicroSkillCount).HasColumnName("total_micro_skill_count");
            entity.Property(e => e.IngestedAt).HasColumnName("ingested_at");
            entity.HasIndex(e => e.OrganisationId);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.UnitId);
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => e.TimeKey);
        });

        modelBuilder.Entity<EdwInterventionFact>(entity =>
        {
            entity.ToTable("fact_intervention");
            entity.HasKey(e => e.EventId);
            entity.ConfigureTenantId();
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.InterventionId).HasColumnName("intervention_id");
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.LearningGapId).HasColumnName("learning_gap_id");
            entity.Property(e => e.AssignedTeacherUserId).HasColumnName("assigned_teacher_user_id").IsRequired();
            entity.Property(e => e.InterventionType).HasColumnName("intervention_type");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.TimeKey).HasColumnName("time_key");
            entity.Property(e => e.IngestedAt).HasColumnName("ingested_at");
            entity.HasIndex(e => e.OrganisationId);
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => e.TimeKey);
        });

        base.OnModelCreating(modelBuilder);
    }
}
