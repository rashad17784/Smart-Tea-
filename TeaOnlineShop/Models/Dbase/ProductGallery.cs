using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class ProductGallery
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string? ImageName { get; set; }
}
