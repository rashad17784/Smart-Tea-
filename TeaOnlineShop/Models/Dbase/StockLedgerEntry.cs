namespace TeaOnlineShop.Models.Dbase;

public sealed class StockLedgerEntry
{
    public long Id { get; set; }
    public Guid EntryNumber { get; set; } = Guid.NewGuid();
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public int WarehouseId { get; set; }
    public int? BinId { get; set; }
    public int? InventoryItemId { get; set; }
    public int? SupplyItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal QuantityChange { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public decimal? UnitCost { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int? PerformedByUserId { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsReversal { get; set; }
    public long? ReversesEntryId { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public WarehouseBin? Bin { get; set; }
    public TeaInventoryItem? InventoryItem { get; set; }
    public SupplyItem? SupplyItem { get; set; }
    public StockLedgerEntry? ReversesEntry { get; set; }
}
