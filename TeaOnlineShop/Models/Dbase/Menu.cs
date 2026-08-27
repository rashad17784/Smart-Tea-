using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class Menu
{
    public int Id { get; set; }

    public string? MenuTitle { get; set; }

    public string? Link { get; set; }

    public string? Type { get; set; }
}
