using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels
{
    public class StockAdjustmentViewModel
    {
        public int ItemId { get; set; }
        
        public string ItemName { get; set; } = string.Empty;
        
        public string TeaType { get; set; } = string.Empty;
        
        public string Grade { get; set; } = string.Empty;
        
        [Display(Name = "Current Stock")]
        public decimal CurrentStock { get; set; }
        
        [Required]
        [Display(Name = "New Stock Level")]
        [Range(0, 9999999, ErrorMessage = "Stock level must be a positive number")]
        public decimal NewStock { get; set; }
        
        [Required]
        [Display(Name = "Reason for Adjustment")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;
    }
} 