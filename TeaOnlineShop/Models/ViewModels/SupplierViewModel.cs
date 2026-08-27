using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Models.ViewModels
{
    public class SupplierViewModel
    {
        // Basic Supplier Info
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Supplier code is required")]
        [Display(Name = "Supplier Code")]
        public string SupplierCode { get; set; } = null!;
        
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = null!;
        
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }
        
        [Phone]
        public string? Phone { get; set; }
        
        [EmailAddress]
        public string? Email { get; set; }
        
        public string? Address { get; set; }
        
        [Display(Name = "Registration Date")]
        [DataType(DataType.Date)]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        
        // QR Code data will be generated automatically
        [Display(Name = "QR Code Data")]
        public string? QRCodeData { get; set; } = string.Empty;
        
        public string Status { get; set; } = "Active";
        
        public string? Notes { get; set; }
        
        // For Category Selection
        [Display(Name = "Categories")]
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();
        
        public List<SelectListItem> AvailableCategories { get; set; } = new List<SelectListItem>();
        
        // For Displaying QR Code
        public string QRCodeImageUrl { get; set; } = string.Empty;
    }
} 