namespace TeaOnlineShop.Authorization;

public sealed class WarehousePermissionOptions
{
    public const string SectionName = "WarehousePermissions";

    public decimal MaximumAdjustmentUnits { get; set; } = 25m;
    public decimal MaximumAdjustmentPercent { get; set; } = 5m;
}
