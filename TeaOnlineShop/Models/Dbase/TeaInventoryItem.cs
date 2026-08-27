using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.Dbase;

public class TeaInventoryItem
{
    public TeaInventoryItem()
    {
        Transactions = new HashSet<TeaInventoryTransaction>();
        CreatedDate = DateTime.Now;
        Status = "Active";
        HasBeenCorrected = false;
        
        // Initialize string properties to empty strings instead of null
        Name = string.Empty;
        TeaType = string.Empty;
        Grade = string.Empty;
        Origin = string.Empty;
        HarvestSeason = string.Empty;
        BatchNumber = string.Empty;
        Description = string.Empty;
        Unit = string.Empty;
        QRCodeData = string.Empty;
        LastCorrectedBy = string.Empty;
        CorrectionReason = string.Empty;
    }

    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(50)]
    public string TeaType { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Grade { get; set; }
    
    [StringLength(100)]
    public string Origin { get; set; }
    
    [StringLength(50)]
    public string HarvestSeason { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? HarvestDate { get; set; }
    
    [StringLength(50)]
    public string BatchNumber { get; set; }
    
    public string Description { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal CurrentStock { get; set; }
    
    [Required]
    [StringLength(10)]
    public string Unit { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? MinimumStock { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? ReorderLevel { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? ReorderQuantity { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? RetailPrice { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; }
    
    [Required]
    [StringLength(100)]
    public string QRCodeData { get; set; }
    
    [Required]
    public DateTime CreatedDate { get; set; }
    
    public DateTime? LastUpdated { get; set; }
    
    public bool HasBeenCorrected { get; set; }
    
    public DateTime? LastCorrectionDate { get; set; }
    
    [StringLength(100)]
    public string? LastCorrectedBy { get; set; }
    
    public string? CorrectionReason { get; set; }
    
    // Navigation property
    public virtual ICollection<TeaInventoryTransaction> Transactions { get; set; }
    public virtual ICollection<ProductInventoryMapping> ProductMappings { get; set; } = new List<ProductInventoryMapping>();
    public virtual ICollection<StockLedgerEntry> LedgerEntries { get; set; } = new List<StockLedgerEntry>();
}
