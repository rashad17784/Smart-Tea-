using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class EnableAuthenticatorViewModel
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string QrCodeImageData { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;
}

public sealed class RecoveryCodesViewModel
{
    public IReadOnlyCollection<string> RecoveryCodes { get; set; } = Array.Empty<string>();
}
