using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WePlatform.Tenancy;

public static class TenantEntityConfigurationExtensions
{
    public static EntityTypeBuilder<TEntity> ConfigureTenantId<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITenantEntity
    {
        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        return builder;
    }
}
