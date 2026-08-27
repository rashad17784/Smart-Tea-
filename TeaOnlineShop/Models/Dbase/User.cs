using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.Dbase;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public bool IfAdmin { get; set; }

    [Display(Name = "User Role")]
    public string UserRole { get; set; } = "Customer"; // Default role

    public DateTime? DateOfRegister { get; set; }

    public int? RecoveryCode { get; set; }
    
    // Helper method to check if user has a specific role
    public bool HasRole(string role)
    {
        if (IfAdmin && role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return true;
            
        return UserRole?.Equals(role, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
