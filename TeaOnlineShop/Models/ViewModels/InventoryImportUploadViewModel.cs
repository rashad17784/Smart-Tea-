using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

public sealed class InventoryImportUploadViewModel
{
    [Required]
    [Display(Name = "Opening-balance CSV file")]
    public IFormFile? File { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
