using Microsoft.EntityFrameworkCore;
using ReportingService.Domain;

namespace ReportingService.Infrastructure.Data;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<GeneratedReport> Reports => Set<GeneratedReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeneratedReport>(entity =>
        {
            entity.HasKey(report => report.Id);
            entity.Property(report => report.ReportType).HasMaxLength(64).IsRequired();
            entity.Property(report => report.RequestedByUserId).HasMaxLength(128).IsRequired();
            entity.Property(report => report.ContentJson).IsRequired();
        });
    }
}
