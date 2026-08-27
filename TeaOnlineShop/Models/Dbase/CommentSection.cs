using System;
using System.Collections.Generic;

namespace TeaOnlineShop.Models.Dbase;

public partial class CommentSection
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string CommmentText { get; set; } = null!;

    public int ProductId { get; set; }

    public DateTime CreateDate { get; set; }
}
