using Microsoft.EntityFrameworkCore;
using AssessmentService.Domain;

namespace AssessmentService.Infrastructure.Data;

public sealed class AssessmentDbContext(DbContextOptions<AssessmentDbContext> options) : DbContext(options)
{
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentLearningObjective> AssessmentLearningObjectives => Set<AssessmentLearningObjective>();
    public DbSet<AssessmentMicroSkill> AssessmentMicroSkills => Set<AssessmentMicroSkill>();
    public DbSet<AssessmentSubmission> Submissions => Set<AssessmentSubmission>();
    public DbSet<AiFeedbackAuditLog> AiFeedbackAuditLogs => Set<AiFeedbackAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("assessments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganisationId).HasColumnName("organisation_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.CreatedByTeacherUserId).HasColumnName("created_by_teacher_user_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.DueAt).HasColumnName("due_at");
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.ClassId);
            entity.HasIndex(e => e.OrganisationId);
            entity.HasIndex(e => e.Status);
            entity.HasMany(e => e.LearningObjectives).WithOne(e => e.Assessment)
                .HasForeignKey(e => e.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.MicroSkills).WithOne(e => e.Assessment)
                .HasForeignKey(e => e.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Submissions).WithOne(e => e.Assessment)
                .HasForeignKey(e => e.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssessmentLearningObjective>(entity =>
        {
            entity.ToTable("assessment_learning_objectives");
            entity.HasKey(e => new { e.AssessmentId, e.LearningObjectiveId });
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.LearningObjectiveId).HasColumnName("learning_objective_id");
        });

        modelBuilder.Entity<AssessmentMicroSkill>(entity =>
        {
            entity.ToTable("assessment_micro_skills");
            entity.HasKey(e => new { e.AssessmentId, e.MicroSkillId });
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.MicroSkillId).HasColumnName("micro_skill_id");
        });

        modelBuilder.Entity<AssessmentSubmission>(entity =>
        {
            entity.ToTable("assessment_submissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.Responses).HasColumnName("responses").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.IsLate).HasColumnName("is_late");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.AssessmentId);
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => new { e.AssessmentId, e.StudentUserId }).IsUnique();
        });

        modelBuilder.Entity<AiFeedbackAuditLog>(entity =>
        {
            entity.ToTable("ai_feedback_audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.MicroSkillId).HasColumnName("micro_skill_id");
            entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id").IsRequired();
            entity.Property(e => e.PromptId).HasColumnName("prompt_id").IsRequired();
            entity.Property(e => e.PromptVersion).HasColumnName("prompt_version").IsRequired();
            entity.Property(e => e.PromptVariablesJson).HasColumnName("prompt_variables_json").IsRequired();
            entity.Property(e => e.AiResponse).HasColumnName("ai_response").IsRequired();
            entity.Property(e => e.TeacherEditedFeedback).HasColumnName("teacher_edited_feedback");
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.FinalApprovedAt).HasColumnName("final_approved_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.SubmissionId);
            entity.HasIndex(e => e.TeacherUserId);
        });
    }
}
