using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.ViewModels
{
    public class DashboardViewModel
    {
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Display(Name = "Date Range")]
        public string DateRange { get; set; } = "last30days";
        
        // Sales metrics
        public decimal TotalSales { get; set; }
        public decimal TotalSalesQuantity { get; set; }
        public decimal TotalProductionQuantity { get; set; }
        public decimal TotalDeliveryQuantity { get; set; }
        
        // Additional sales metrics
        public decimal AverageDailySales { get; set; }
        public int SalesTransactionCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        
        // Chart data
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<double> SalesChartData { get; set; } = new List<double>();
        public List<double> ProductionChartData { get; set; } = new List<double>();
        
        // Pie chart data for sales by tea type
        public List<string> PieChartLabels { get; set; } = new List<string>();
        public List<double> PieChartData { get; set; } = new List<double>();
        
        // Top selling items
        public List<TopSellingItemViewModel> TopSellingItems { get; set; } = new List<TopSellingItemViewModel>();
    }
    
    public class TopSellingItemViewModel
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TeaType { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
} 