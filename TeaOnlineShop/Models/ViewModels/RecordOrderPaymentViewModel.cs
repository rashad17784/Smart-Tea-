using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class RecordOrderPaymentViewModel
{
    public int OrderId { get; set; }

    [Required, StringLength(120, MinimumLength = 3)]
    public string Reference { get; set; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}
