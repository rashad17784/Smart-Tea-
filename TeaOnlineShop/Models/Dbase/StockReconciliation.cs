namespace TeaOnlineShop.Models.Dbase;

public sealed class StockReconciliation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReconciliationNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CountedAtUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? Notes { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<StockReconciliationLine> Lines { get; set; } = new List<StockReconciliationLine>();
}
