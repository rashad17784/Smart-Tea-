using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.Dbase;

public sealed class OrderPaymentEvent
{
    public long Id { get; set; }
    public int OrderId { get; set; }

    [StringLength(40)]
    public string FromStatus { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string ToStatus { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Method { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [Required, StringLength(120)]
    public string Reference { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public int? RecordedByUserId { get; set; }

    [Required, StringLength(120)]
    public string RecordedByName { get; set; } = string.Empty;

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public Order Order { get; set; } = null!;
}
