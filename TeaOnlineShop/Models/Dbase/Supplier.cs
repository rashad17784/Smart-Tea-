using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class Supplier
{
    public int Id { get; set; }
    public string SupplierCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
    public string QRCodeData { get; set; } = null!;
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public virtual ICollection<SupplierCategoryMapping> CategoryMappings { get; set; } = new List<SupplierCategoryMapping>();
} 