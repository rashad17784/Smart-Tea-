using Microsoft.AspNetCore.Identity;

namespace TeaOnlineShop.Identity;

public sealed class ApplicationUser : IdentityUser<int>
{
    public int? LegacyUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool RequiresPasswordChange { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}
