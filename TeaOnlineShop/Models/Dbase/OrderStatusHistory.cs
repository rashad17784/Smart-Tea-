namespace TeaOnlineShop.Models.Dbase;

public sealed class OrderStatusHistory
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}
