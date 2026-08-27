using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels
{
    public class SalesReportViewModel
    {
        public string Title { get; set; } = "Sales Report";
        
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        public DateTime GeneratedDate { get; set; }
        
        // Summary metrics
        public decimal TotalSales { get; set; }
        public decimal TotalItemsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        
        // Detailed data
        public List<DailySalesSummary> DailySales { get; set; } = new List<DailySalesSummary>();
        public List<TeaTypeSalesSummary> SalesByTeaType { get; set; } = new List<TeaTypeSalesSummary>();
        public List<TopSellingProductSummary> TopSellingProducts { get; set; } = new List<TopSellingProductSummary>();
        
        // Error handling
        public string ErrorMessage { get; set; }
    }
    
    public class DailySalesSummary
    {
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ItemsSold { get; set; }
        public int TransactionCount { get; set; }
    }
    
    public class TeaTypeSalesSummary
    {
        public string TeaType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ItemsSold { get; set; }
        public double Percentage { get; set; }
    }
    
    public class TopSellingProductSummary
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string TeaType { get; set; }
        public string Grade { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ItemsSold { get; set; }
    }
} 