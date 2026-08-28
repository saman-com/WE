using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
