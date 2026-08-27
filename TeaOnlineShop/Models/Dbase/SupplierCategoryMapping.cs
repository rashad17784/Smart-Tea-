using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class SupplierCategoryMapping
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int CategoryId { get; set; }

    // Navigation properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual SupplierCategory Category { get; set; } = null!;
} 