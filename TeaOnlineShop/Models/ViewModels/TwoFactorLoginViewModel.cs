using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class TwoFactorLoginViewModel
{
    [Required]
    [Display(Name = "Authenticator code")]
    public string Code { get; set; } = string.Empty;

    public bool RememberMachine { get; set; }
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
