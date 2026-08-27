namespace TeaOnlineShop.Models.Dbase;

public sealed class ProductInventoryMapping
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int InventoryItemId { get; set; }
    public decimal QuantityPerUnit { get; set; } = 1m;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
    public TeaInventoryItem InventoryItem { get; set; } = null!;
}
