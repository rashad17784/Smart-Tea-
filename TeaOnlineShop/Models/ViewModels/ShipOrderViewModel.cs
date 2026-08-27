using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class ShipOrderViewModel
{
    public int OrderId { get; set; }

    [Required, StringLength(100)]
    public string Carrier { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string TrackingNumber { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Reason { get; set; } = "Packed, verified, and handed to the carrier.";
}
