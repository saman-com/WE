using AiGatewayService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiGatewayService.Infrastructure.Data;

public sealed class AiGatewayDbContext(DbContextOptions<AiGatewayDbContext> options) : DbContext(options)
{
    public DbSet<AiAuditLog> AiAuditLogs => Set<AiAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiAuditLog>(entity =>
        {
            entity.ToTable("ai_audit_logs");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.CallerUserId).IsRequired();
            entity.Property(entry => entry.PromptId).IsRequired();
            entity.Property(entry => entry.PromptVersion).IsRequired();
            entity.Property(entry => entry.ContextScope).IsRequired();
            entity.Property(entry => entry.PromptVariablesJson).IsRequired();
            entity.Property(entry => entry.AiResponse).IsRequired();
            entity.Property(entry => entry.Outcome).IsRequired();
            entity.Property(entry => entry.ProviderName).IsRequired();
            entity.Property(entry => entry.CreatedAt).IsRequired();
        });
    }

    public override int SaveChanges()
    {
        EnforceAppendOnlyAuditLogs();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceAppendOnlyAuditLogs();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void EnforceAppendOnlyAuditLogs()
    {
        foreach (var entry in ChangeTracker.Entries<AiAuditLog>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("AI audit logs are append-only.");
            }
        }
    }
}
