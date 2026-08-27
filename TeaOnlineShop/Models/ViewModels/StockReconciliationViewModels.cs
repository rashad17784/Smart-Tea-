using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class StockReconciliationCreateViewModel
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<StockReconciliationCountLineViewModel> Lines { get; set; } = new();
}

public sealed class StockReconciliationCountLineViewModel
{
    public long BalanceId { get; set; }
    public int? InventoryItemId { get; set; }
    public int? SupplyItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string BinCode { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal? CountedQuantity { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
