namespace TeaOnlineShop.Models.ViewModels;

public sealed class InventoryIntegrityViewModel
{
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    public List<InventoryIntegrityIssueViewModel> Issues { get; set; } = new();
    public int LedgerEntries { get; set; }
    public int StockBalances { get; set; }
    public int ProductMappings { get; set; }
    public int OrdersWithoutLines { get; set; }
    public int ReceivedDeliveriesWithoutLedger { get; set; }
}

public sealed class InventoryIntegrityIssueViewModel
{
    public string Severity { get; set; } = "Warning";
    public string Entity { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
