namespace TeaOnlineShop.Models.Dbase;

public sealed class OrderLine
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string FulfilmentStatus { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
    public Product? Product { get; set; }
}
