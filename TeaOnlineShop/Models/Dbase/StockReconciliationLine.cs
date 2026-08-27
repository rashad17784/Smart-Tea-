namespace TeaOnlineShop.Models.Dbase;

public sealed class StockReconciliationLine
{
    public long Id { get; set; }
    public Guid ReconciliationId { get; set; }
    public int? InventoryItemId { get; set; }
    public int? SupplyItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Difference { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long? LedgerEntryId { get; set; }

    public StockReconciliation Reconciliation { get; set; } = null!;
    public StockLedgerEntry? LedgerEntry { get; set; }
}
