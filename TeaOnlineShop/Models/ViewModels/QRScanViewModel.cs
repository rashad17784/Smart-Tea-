using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels
{
    public class QRScanViewModel
    {
        [Required]
        public string QRCodeData { get; set; } = string.Empty;
        
        public string ScanResult { get; set; } = string.Empty;
        
        public bool SupplierFound { get; set; } = false;
        
        public int? SupplierId { get; set; }
        
        public string SupplierName { get; set; } = string.Empty;
        
        public string SupplierCode { get; set; } = string.Empty;
        
        public string ContactPerson { get; set; } = string.Empty;
        
        public string Phone { get; set; } = string.Empty;
        
        public string Email { get; set; } = string.Empty;
    }
} 