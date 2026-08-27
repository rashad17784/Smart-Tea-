using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Models.ViewModels
{
    public class DeliveryViewModel
    {
        // Basic Delivery Info
        public int Id { get; set; }
        
        [Display(Name = "Delivery Code")]
        public string DeliveryCode { get; set; } = null!;
        
        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
        
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;
        
        [Display(Name = "Delivery Date")]
        [DataType(DataType.DateTime)]
        public DateTime DeliveryDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal? TotalAmount { get; set; }
        
        public string Status { get; set; } = "Received";
        
        public string? Notes { get; set; }
        
        // For dropdowns
        public List<SelectListItem> SuppliersList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> SupplyItemsList { get; set; } = new List<SelectListItem>();
        
        // For items in the delivery
        public List<DeliveryItemViewModel> Items { get; set; } = new List<DeliveryItemViewModel>();
        
        // For QR Code scanning result
        public string QRCodeData { get; set; } = string.Empty;
    }
    
    public class DeliveryItemViewModel
    {
        public int Id { get; set; }
        
        public int DeliveryId { get; set; }
        
        [Required(ErrorMessage = "Item is required")]
        [Display(Name = "Item")]
        public int ItemId { get; set; }
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }
        
        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public decimal? UnitPrice { get; set; }
        
        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public decimal? TotalPrice { get; set; }
        
        public string? Notes { get; set; }
        
        // For display
        public string ItemName { get; set; } = string.Empty;
        public string ItemCategory { get; set; } = string.Empty;
        public string ItemUnit { get; set; } = string.Empty;
    }
} 