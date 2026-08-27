using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class SupplyItem
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public string? Description { get; set; }
    public decimal? MinimumStock { get; set; }
    public decimal CurrentStock { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();
    public virtual ICollection<StockLedgerEntry> LedgerEntries { get; set; } = new List<StockLedgerEntry>();
}
