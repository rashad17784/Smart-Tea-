namespace TeaOnlineShop.Authorization;

public static class WarehouseAdjustmentPolicy
{
    public static decimal MaximumAllowedChange(
        decimal currentStock,
        WarehousePermissionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var percentageLimit = Math.Max(
            1m,
            Math.Abs(currentStock) * options.MaximumAdjustmentPercent / 100m);

        return Math.Min(options.MaximumAdjustmentUnits, percentageLimit);
    }

    public static bool IsWithinLimit(
        decimal currentStock,
        decimal requestedNewStock,
        WarehousePermissionOptions options) =>
        Math.Abs(requestedNewStock - currentStock) <= MaximumAllowedChange(currentStock, options);
}
