using EdwIngestService.Domain;
using Microsoft.EntityFrameworkCore;
using WePlatform.Tenancy;

namespace EdwIngestService.Infrastructure.Data;

public sealed class EdwDbContext(
    DbContextOptions<EdwDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<DimTime> DimTimes => Set<DimTime>();
    public DbSet<EvidenceFact> EvidenceFacts => Set<EvidenceFact>();
    public DbSet<AssessmentFact> AssessmentFacts => Set<AssessmentFact>();
    public DbSet<InterventionFact> InterventionFacts => Set<InterventionFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimTime>(entity =>
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

        modelBuilder.Entity<EvidenceFact>(entity =>
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
            entity.Property(e => e.IngestedAt).HasColumnName("ingested_at");
            entity.HasIndex(e => e.OrganisationId);
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => e.TimeKey);
        });

        modelBuilder.Entity<AssessmentFact>(entity =>
        {
            entity.ToTable("fact_assessment");
            entity.HasKey(e => e.EventId);
            entity.ConfigureTenantId();
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.ApprovedByTeacherUserId).HasColumnName("approved_by_teacher_user_id").IsRequired();
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.TimeKey).HasColumnName("time_key");
            entity.Property(e => e.MicroSkillCount).HasColumnName("micro_skill_count");
            entity.Property(e => e.IngestedAt).HasColumnName("ingested_at");
            entity.HasIndex(e => e.OrganisationId);
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => e.TimeKey);
        });

        modelBuilder.Entity<InterventionFact>(entity =>
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
