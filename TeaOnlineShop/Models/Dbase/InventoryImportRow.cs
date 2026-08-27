namespace TeaOnlineShop.Models.Dbase;

public sealed class InventoryImportRow
{
    public long Id { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string WarehouseCode { get; set; } = "MAIN";
    public string BinCode { get; set; } = "DEFAULT";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Validated";
    public long? LedgerEntryId { get; set; }

    public InventoryImportBatch Batch { get; set; } = null!;
    public StockLedgerEntry? LedgerEntry { get; set; }
}
