using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class SupplierCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<SupplierCategoryMapping> SupplierMappings { get; set; } = new List<SupplierCategoryMapping>();
} 