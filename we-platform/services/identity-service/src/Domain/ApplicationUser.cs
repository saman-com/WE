using Microsoft.AspNetCore.Identity;
using WePlatform.Tenancy;

namespace IdentityService.Domain;

public sealed class ApplicationUser : IdentityUser, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? FederationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
