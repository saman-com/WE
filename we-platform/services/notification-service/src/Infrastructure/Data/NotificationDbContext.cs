using Microsoft.EntityFrameworkCore;
using NotificationService.Domain;
using WePlatform.Tenancy;

namespace NotificationService.Infrastructure.Data;

public sealed class NotificationDbContext(
    DbContextOptions<NotificationDbContext> options,
    ITenantContext tenantContext) : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.ConfigureTenantId();
            entity.Property(e => e.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Body).HasColumnName("body").IsRequired();
            entity.Property(e => e.SourceEventId).HasColumnName("source_event_id");
            entity.Property(e => e.SourceEventType).HasColumnName("source_event_type").IsRequired();
            entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.RecipientUserId);
            entity.HasIndex(e => new { e.SourceEventId, e.RecipientUserId }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
