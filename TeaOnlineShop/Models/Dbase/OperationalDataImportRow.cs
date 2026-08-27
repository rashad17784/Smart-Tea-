namespace TeaOnlineShop.Models.Dbase;

public sealed class OperationalDataImportRow
{
    public long Id { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceRecordId { get; set; } = string.Empty;
    public DateTime OriginalTransactionAtUtc { get; set; }
    public string TeaGrade { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public int? InventoryItemId { get; set; }
    public decimal QuantityKg { get; set; }
    public string OriginalUnit { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal QuantityChangeKg { get; set; }
    public bool IsDemand { get; set; }
    public string SourceReferenceNumber { get; set; } = string.Empty;
    public string SupplierOrProductionReference { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string BinCode { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CanonicalSha256 { get; set; } = string.Empty;
    public string RawData { get; set; } = string.Empty;
    public string Status { get; set; } = "Staged";

    public OperationalDataImportBatch Batch { get; set; } = null!;
    public TeaInventoryItem? InventoryItem { get; set; }
}
