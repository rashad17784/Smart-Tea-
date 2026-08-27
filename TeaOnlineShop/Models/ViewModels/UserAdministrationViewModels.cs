using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class ManagedUserViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool MfaRequired { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class UserManagementIndexViewModel
{
    public IReadOnlyCollection<ManagedUserViewModel> Users { get; set; } = Array.Empty<ManagedUserViewModel>();
    public IReadOnlyCollection<string> AvailableInternalRoles { get; set; } = Array.Empty<string>();
}

public sealed class CreateStaffUserViewModel
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 12)]
    [Display(Name = "Temporary password")]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Staff role")]
    public string Role { get; set; } = string.Empty;
}

// Retained so existing links/bookmarks to the original warehouse-only page remain compatible.
public sealed class CreateWarehouseUserViewModel
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 12)]
    [Display(Name = "Temporary password")]
    public string TemporaryPassword { get; set; } = string.Empty;
}

public sealed class ChangeUserRoleViewModel
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}
