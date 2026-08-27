namespace TeaOnlineShop.Models.Dbase;

public sealed class WarehouseBin
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Warehouse Warehouse { get; set; } = null!;
}
