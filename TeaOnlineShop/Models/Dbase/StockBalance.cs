namespace TeaOnlineShop.Models.Dbase;

public sealed class StockBalance
{
    public long Id { get; set; }
    public int WarehouseId { get; set; }
    public int BinId { get; set; }
    public int? InventoryItemId { get; set; }
    public int? SupplyItemId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Warehouse Warehouse { get; set; } = null!;
    public WarehouseBin Bin { get; set; } = null!;
}
