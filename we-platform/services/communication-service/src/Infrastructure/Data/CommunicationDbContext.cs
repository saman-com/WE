using CommunicationService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunicationService.Infrastructure.Data;

public sealed class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : DbContext(options)
{
    public DbSet<ParentTeacherMessage> Messages => Set<ParentTeacherMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ParentTeacherMessage>(entity =>
        {
            entity.ToTable("parent_teacher_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentUserId).HasColumnName("student_user_id").IsRequired();
            entity.Property(e => e.ParentUserId).HasColumnName("parent_user_id").IsRequired();
            entity.Property(e => e.TeacherUserId).HasColumnName("teacher_user_id").IsRequired();
            entity.Property(e => e.SenderUserId).HasColumnName("sender_user_id").IsRequired();
            entity.Property(e => e.SenderRole).HasColumnName("sender_role").IsRequired();
            entity.Property(e => e.Body).HasColumnName("body").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.StudentUserId);
            entity.HasIndex(e => e.TeacherUserId);
            entity.HasIndex(e => e.ParentUserId);
            entity.HasIndex(e => new { e.StudentUserId, e.ParentUserId, e.TeacherUserId });
        });
    }
}
