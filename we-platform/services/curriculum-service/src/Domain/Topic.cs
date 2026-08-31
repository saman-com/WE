namespace CurriculumService.Domain;

using WePlatform.Tenancy;

public sealed class Topic : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Unit Unit { get; set; } = null!;
}
