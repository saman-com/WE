namespace WePlatform.Tenancy;

public interface IOrganisationTenantEntity : ITenantEntity
{
    Guid OrganisationId { get; }
}

public static class TenantBackfill
{
    public static Guid ResolveOrganisationTenant(IOrganisationTenantEntity entity) =>
        entity.OrganisationId == Guid.Empty ? DefaultTenant.Id : entity.OrganisationId;

    public static Guid ResolveSelfTenant(Guid entityId) =>
        entityId == Guid.Empty ? DefaultTenant.Id : entityId;
}
