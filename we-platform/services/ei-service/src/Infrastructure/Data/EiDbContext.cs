using EiService.Domain;
using Microsoft.EntityFrameworkCore;

namespace EiService.Infrastructure.Data;

public sealed class EiDbContext(DbContextOptions<EiDbContext> options) : DbContext(options)
{
    public DbSet<AiSummaryAuditLog> AiSummaryAuditLogs => Set<AiSummaryAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiSummaryAuditLog>(entity =>
        {
            entity.ToTable("ai_summary_audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SummaryType).IsRequired();
            entity.Property(e => e.TeacherUserId).IsRequired();
            entity.Property(e => e.PromptId).IsRequired();
            entity.Property(e => e.PromptVersion).IsRequired();
            entity.Property(e => e.PromptVariablesJson).IsRequired();
            entity.Property(e => e.ContextScopeJson).IsRequired();
            entity.Property(e => e.AiResponse).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
