using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels
{
    public class StockTransactionViewModel
    {
        public int ItemId { get; set; }
        
        public string ItemName { get; set; } = string.Empty;
        
        public string TeaType { get; set; } = string.Empty;
        
        public string Grade { get; set; } = string.Empty;
        
        [Display(Name = "Current Stock")]
        public decimal CurrentStock { get; set; }
        
        [Required]
        [Display(Name = "Transaction Type")]
        public string TransactionType { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Quantity")]
        [Range(0.01, 9999999, ErrorMessage = "Quantity must be greater than zero")]
        public decimal Quantity { get; set; }
        
        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public decimal? UnitPrice { get; set; }
        
        [Display(Name = "Reference Number")]
        [StringLength(50, ErrorMessage = "Reference number cannot exceed 50 characters")]
        public string? ReferenceNumber { get; set; }
        
        [Display(Name = "Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }
} 