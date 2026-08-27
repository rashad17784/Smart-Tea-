using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class DeliveryItem
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Delivery Delivery { get; set; } = null!;
    public virtual SupplyItem Item { get; set; } = null!;
} 