namespace TeaOnlineShop.Models.Dbase;

public sealed class OperationalInventoryEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public long ImportRowId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string SourceRecordId { get; set; } = string.Empty;
    public DateTime SourceOccurredAtUtc { get; set; }
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public string TeaGrade { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public int InventoryItemId { get; set; }
    public decimal QuantityKg { get; set; }
    public decimal QuantityChangeKg { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public bool IsDemand { get; set; }
    public string SourceReferenceNumber { get; set; } = string.Empty;
    public string SupplierOrProductionReference { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string BinCode { get; set; } = string.Empty;
    public decimal? UnitCost { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CanonicalSha256 { get; set; } = string.Empty;
    public int ImportedByUserId { get; set; }
    public string ImportedByName { get; set; } = string.Empty;

    public OperationalDataImportBatch Batch { get; set; } = null!;
    public OperationalDataImportRow ImportRow { get; set; } = null!;
    public TeaInventoryItem InventoryItem { get; set; } = null!;
}
