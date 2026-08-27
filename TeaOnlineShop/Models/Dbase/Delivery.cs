using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class Delivery
{
    public int Id { get; set; }
    public string DeliveryCode { get; set; } = null!;
    public int SupplierId { get; set; }
    public int ReceivedById { get; set; }
    public string ReceivedByName { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; } = DateTime.Now;
    public decimal? TotalAmount { get; set; }
    public string Status { get; set; } = "Received";
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<DeliveryItem> DeliveryItems { get; set; } = new List<DeliveryItem>();
} 
