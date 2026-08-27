using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.Dbase;

public partial class Product
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? FullDescription { get; set; }

    public decimal? Price { get; set; }

    public decimal? Discount { get; set; }

    public string? ImageName { get; set; }

    public int Quantity { get; set; } = 0;

    public string? Tags { get; set; }

    public string? VideoUrl { get; set; }

    public virtual IEnumerable<ProductGallery>? ProductGalleries { get; set; }

    public virtual ProductInventoryMapping? InventoryMapping { get; set; }
    public virtual ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}
