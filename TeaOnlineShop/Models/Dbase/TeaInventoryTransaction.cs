using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaOnlineShop.Models.Dbase;

public class TeaInventoryTransaction
{
    public TeaInventoryTransaction()
    {
        TransactionDate = DateTime.Now;
        IsCorrection = false;
        
        // Initialize string properties to empty strings instead of null
        TransactionType = string.Empty;
        ReferenceNumber = string.Empty;
        Notes = string.Empty;
        PerformedBy = string.Empty;
        CorrectionReason = string.Empty;
        QRCodeScanned = string.Empty;
    }

    public int Id { get; set; }
    
    [Required]
    public int InventoryItemId { get; set; }
    
    // Add backward compatibility property for code that still uses TeaInventoryItemId
    [NotMapped]
    public int TeaInventoryItemId 
    { 
        get => InventoryItemId; 
        set => InventoryItemId = value; 
    }
    
    public int? ReferenceId { get; set; } // ID of related order, delivery, etc.
    
    [Required]
    public DateTime TransactionDate { get; set; }
    
    [Required]
    [StringLength(50)]
    public string TransactionType { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal PreviousStock { get; set; }
    
    [Required]
    [Range(0, double.MaxValue)]
    public decimal NewStock { get; set; }
    
    [StringLength(50)]
    public string ReferenceNumber { get; set; }
    
    public decimal? UnitPrice { get; set; }
    
    public string Notes { get; set; }
    
    [StringLength(100)]
    public string PerformedBy { get; set; }
    
    public bool IsCorrection { get; set; }
    
    public string CorrectionReason { get; set; }
    
    public string QRCodeScanned { get; set; }
    
    public int? RelatedTransactionId { get; set; } // For corrections or reversals
    
    // Navigation property for new code
    public virtual TeaInventoryItem InventoryItem { get; set; }
    
    // Backward compatibility property for code that still uses TeaInventoryItem
    [NotMapped]
    public virtual TeaInventoryItem TeaInventoryItem 
    { 
        get => InventoryItem; 
        set => InventoryItem = value; 
    }
} 