namespace TeaOnlineShop.Models.Dbase;

public sealed class Warehouse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WarehouseBin> Bins { get; set; } = new List<WarehouseBin>();
    public ICollection<StockLedgerEntry> LedgerEntries { get; set; } = new List<StockLedgerEntry>();
}
