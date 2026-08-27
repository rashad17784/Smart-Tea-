using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels;

/// <summary>
/// Accepts only the fields an administrator is allowed to supply when a tea
/// inventory master item is created. Audit, QR and ledger fields are generated
/// by the server.
/// </summary>
public sealed class TeaInventoryCreateViewModel
{
    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]{1,49}$",
        ErrorMessage = "Use 2-50 letters, numbers, dots, underscores or hyphens (for example TEA-BOP).")]
    [Display(Name = "Item code")]
    public string ItemCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "Tea type")]
    public string TeaType { get; set; } = "Black";

    [Required, StringLength(50)]
    public string Grade { get; set; } = "BOP";

    [Required, StringLength(10)]
    public string Unit { get; set; } = "kg";

    [StringLength(100)]
    public string Origin { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Harvest season")]
    public string HarvestSeason { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Harvest date")]
    public DateTime? HarvestDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Batch number")]
    public string BatchNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [Display(Name = "Initial stock")]
    public decimal InitialStock { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [Display(Name = "Minimum stock")]
    public decimal? MinimumStock { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [Display(Name = "Reorder level")]
    public decimal? ReorderLevel { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [Display(Name = "Reorder quantity")]
    public decimal? ReorderQuantity { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [Display(Name = "Unit cost")]
    public decimal? UnitCost { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [Display(Name = "Retail price")]
    public decimal? RetailPrice { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";
}
