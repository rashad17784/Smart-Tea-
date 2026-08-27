using Microsoft.AspNetCore.Mvc;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Services;
using TeaOnlineShop.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using TeaOnlineShop.Authorization;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    public class ReportsController : AdminBaseController
    {
        private readonly TeaOnlineShopContext _context;
        private readonly InventoryService _inventoryService;

        public ReportsController(TeaOnlineShopContext context, InventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        // GET: Admin/Reports
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public IActionResult Index()
        {
            return View();
        }

        // GET: Admin/Reports/Dashboard
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public IActionResult Dashboard()
        {
            // Get current date and calculate date range for reports
            var today = DateTime.Today;
            var startDate = today.AddDays(-30);
            var endDate = today;

            // Create the dashboard view model
            var dashboardViewModel = new DashboardViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                DateRange = "last30days"
            };

            // Populate the dashboard with data
            PopulateDashboardData(dashboardViewModel);

            return View(dashboardViewModel);
        }

        // POST: Admin/Reports/Dashboard
        [HttpPost]
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public IActionResult Dashboard(DashboardViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Set date range based on selection
                switch (model.DateRange)
                {
                    case "today":
                        model.StartDate = DateTime.Today;
                        model.EndDate = DateTime.Today;
                        break;
                    case "yesterday":
                        model.StartDate = DateTime.Today.AddDays(-1);
                        model.EndDate = DateTime.Today.AddDays(-1);
                        break;
                    case "last7days":
                        model.StartDate = DateTime.Today.AddDays(-7);
                        model.EndDate = DateTime.Today;
                        break;
                    case "last30days":
                        model.StartDate = DateTime.Today.AddDays(-30);
                        model.EndDate = DateTime.Today;
                        break;
                    case "thismonth":
                        model.StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        model.EndDate = DateTime.Today;
                        break;
                    case "lastmonth":
                        model.StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                        model.EndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
                        break;
                    case "custom":
                        // Custom date range already set by the form, but ensure they're valid
                        if (model.EndDate < model.StartDate)
                        {
                            // Swap dates if end date is before start date
                            var temp = model.StartDate;
                            model.StartDate = model.EndDate;
                            model.EndDate = temp;
                        }
                        // Limit date range to 90 days to prevent performance issues
                        if ((model.EndDate - model.StartDate).TotalDays > 90)
                        {
                            model.StartDate = model.EndDate.AddDays(-90);
                            TempData["InfoMessage"] = "Date range limited to 90 days for better performance.";
                        }
                        break;
                    default:
                        model.StartDate = DateTime.Today.AddDays(-30);
                        model.EndDate = DateTime.Today;
                        model.DateRange = "last30days";
                        break;
                }

                // Log the selected date range
                System.Diagnostics.Debug.WriteLine($"Dashboard date range: {model.DateRange}, {model.StartDate:yyyy-MM-dd} to {model.EndDate:yyyy-MM-dd}");

                // Populate the dashboard with data
                PopulateDashboardData(model);
            }
            else
            {
                // If model state is invalid, log errors
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                System.Diagnostics.Debug.WriteLine($"Dashboard model validation errors: {errors}");
            }

            return View(model);
        }

        // GET: Admin/Reports/InventoryReport
        [Authorize(Policy = AppPermissions.InventoryView)]
        public async Task<IActionResult> InventoryReport()
        {
            var items = await _context.TeaInventoryItems
                .OrderBy(i => i.TeaType)
                .ThenBy(i => i.Grade)
                .ToListAsync();

            return View(items);
        }

        // GET: Admin/Reports/TransactionLog
        [Authorize(Policy = AppPermissions.AuditView)]
        public async Task<IActionResult> TransactionLog()
        {
            var transactions = await _context.TeaInventoryTransactions
                .Include(t => t.InventoryItem)
                .OrderByDescending(t => t.TransactionDate)
                .Take(100) // Limit to most recent 100 transactions
                .ToListAsync();

            return View(transactions);
        }

        // GET: Admin/Reports/ExportPdf
        [Authorize(Policy = AppPermissions.InventoryView)]
        public async Task<IActionResult> ExportPdf(string reportType)
        {
            try
            {
                if (string.Equals(reportType, "analytics", StringComparison.OrdinalIgnoreCase) &&
                    !User.HasClaim(AppPermissions.ClaimType, AppPermissions.DashboardFinancialView))
                {
                    return Forbid();
                }
                if (!User.HasClaim(AppPermissions.ClaimType, AppPermissions.DashboardFinancialView))
                {
                    return Forbid();
                }
                byte[] fileBytes;
                string fileName;

                // Generate the appropriate file based on report type
                if (reportType.ToLower() == "inventory")
                {
                    var items = await _context.TeaInventoryItems
                        .OrderBy(i => i.TeaType)
                        .ThenBy(i => i.Grade)
                        .ToListAsync();

                    var pdfService = HttpContext.RequestServices.GetRequiredService<PdfService>();
                    fileBytes = await pdfService.GenerateInventoryReport(items);
                    fileName = $"Inventory_Report_{DateTime.Now:yyyyMMdd}.pdf";
                }
                else if (reportType.ToLower() == "transactions")
                {
                    var transactions = await _context.TeaInventoryTransactions
                        .Include(t => t.InventoryItem)
                        .OrderByDescending(t => t.TransactionDate)
                        .Take(100) // Limit to most recent 100 transactions
                        .ToListAsync();

                    var pdfService = HttpContext.RequestServices.GetRequiredService<PdfService>();
                    fileBytes = await pdfService.GenerateTransactionReport(transactions);
                    fileName = $"Transaction_Log_{DateTime.Now:yyyyMMdd}.pdf";
                }
                else if (reportType.ToLower() == "analytics")
                {
                    // Get current date range from session or use default
                    var startDate = TempData["ReportStartDate"] != null 
                        ? Convert.ToDateTime(TempData["ReportStartDate"]) 
                        : DateTime.Today.AddDays(-30);
                    
                    var endDate = TempData["ReportEndDate"] != null 
                        ? Convert.ToDateTime(TempData["ReportEndDate"]) 
                        : DateTime.Today;

                    // Create and populate dashboard model
                    var dashboardModel = new DashboardViewModel
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        DateRange = "custom"
                    };
                    
                    PopulateDashboardData(dashboardModel);
                    
                    // Generate the PDF
                    var pdfService = HttpContext.RequestServices.GetRequiredService<PdfService>();
                    fileBytes = await pdfService.GenerateAnalyticsDashboardReport(dashboardModel);
                    fileName = $"Analytics_Dashboard_Report_{DateTime.Now:yyyyMMdd}.pdf";
                }
                else
                {
                    TempData["InfoMessage"] = $"Unknown report type: {reportType}";
                    return RedirectToAction("Dashboard");
                }

                // Return the file
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error exporting file: {ex}");
                TempData["InfoMessage"] = $"Error generating {reportType} report: {ex.Message}";
                
                return RedirectToAction(reportType == "inventory" ? "InventoryReport" : 
                                      reportType == "transactions" ? "TransactionLog" : "Dashboard");
            }
        }

        // GET: Admin/Reports/ExportAnalyticsPdf
        [HttpGet]
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public IActionResult ExportAnalyticsPdf(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Store the date range in TempData for use in the ExportPdf action
                TempData["ReportStartDate"] = startDate;
                TempData["ReportEndDate"] = endDate;
                
                // Redirect to the ExportPdf action with analytics report type
                return RedirectToAction("ExportPdf", new { reportType = "analytics" });
            }
            catch (Exception ex)
            {
                TempData["InfoMessage"] = $"Error exporting PDF: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Admin/Reports/SalesReport
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public IActionResult SalesReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Set default date range if not provided
            var today = DateTime.Today;
            var start = startDate ?? today.AddDays(-30);
            var end = endDate ?? today;

            // Create sales report view model
            var salesReportViewModel = new SalesReportViewModel
            {
                StartDate = start,
                EndDate = end,
                Title = "Sales Report",
                GeneratedDate = DateTime.Now
            };

            try
            {
                // Get sales transactions from the database
                var salesTransactions = _context.TeaInventoryTransactions
                    .Where(t => t.TransactionType == "Sale" && 
                           t.TransactionDate >= start && 
                           t.TransactionDate <= end.AddDays(1))
                    .Include(t => t.InventoryItem)
                    .ToList();

                // Calculate summary metrics
                salesReportViewModel.TotalSales = salesTransactions.Sum(t => t.Quantity * (t.UnitPrice ?? 0));
                salesReportViewModel.TotalItemsSold = salesTransactions.Sum(t => t.Quantity);
                salesReportViewModel.AverageOrderValue = salesTransactions.Count > 0 ? 
                    salesReportViewModel.TotalSales / salesTransactions.Select(t => t.ReferenceNumber).Distinct().Count() : 0;
                
                // Group sales by day
                var salesByDate = salesTransactions
                    .GroupBy(t => t.TransactionDate.Date)
                    .Select(g => new DailySalesSummary
                    {
                        Date = g.Key,
                        TotalAmount = g.Sum(t => t.Quantity * (t.UnitPrice ?? 0)),
                        ItemsSold = g.Sum(t => t.Quantity),
                        TransactionCount = g.Select(t => t.ReferenceNumber).Distinct().Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();
                
                salesReportViewModel.DailySales = salesByDate;
                
                // Group sales by item type
                var salesByType = salesTransactions
                    .GroupBy(t => t.InventoryItem.TeaType)
                    .Select(g => new TeaTypeSalesSummary
                    {
                        TeaType = g.Key,
                        TotalAmount = g.Sum(t => t.Quantity * (t.UnitPrice ?? 0)),
                        ItemsSold = g.Sum(t => t.Quantity),
                        Percentage = 0 // Will calculate below
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();
                
                // Calculate percentage for each type
                if (salesReportViewModel.TotalSales > 0)
                {
                    foreach (var type in salesByType)
                    {
                        type.Percentage = (double)((type.TotalAmount / salesReportViewModel.TotalSales) * 100);
                    }
                }
                
                salesReportViewModel.SalesByTeaType = salesByType;
                
                // Top selling products
                var topProducts = salesTransactions
                    .GroupBy(t => new { t.InventoryItemId, t.InventoryItem.Name, t.InventoryItem.TeaType, t.InventoryItem.Grade })
                    .Select(g => new TopSellingProductSummary
                    {
                        ProductId = g.Key.InventoryItemId,
                        ProductName = g.Key.Name,
                        TeaType = g.Key.TeaType,
                        Grade = g.Key.Grade,
                        TotalAmount = g.Sum(t => t.Quantity * (t.UnitPrice ?? 0)),
                        ItemsSold = g.Sum(t => t.Quantity)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(10)
                    .ToList();
                
                salesReportViewModel.TopSellingProducts = topProducts;
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error generating sales report: {ex}");
                TempData["InfoMessage"] = $"Error generating sales report: {ex.Message}";
                salesReportViewModel.ErrorMessage = ex.Message;
            }

            return View(salesReportViewModel);
        }

        // GET: Admin/Reports/ExportSalesReport
        [Authorize(Policy = AppPermissions.DashboardFinancialView)]
        public async Task<IActionResult> ExportSalesReport(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Create and populate sales report view model for PDF generation
                var salesReportViewModel = new SalesReportViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Title = "Sales Report",
                    GeneratedDate = DateTime.Now
                };

                // Get sales transactions from the database
                var salesTransactions = await _context.TeaInventoryTransactions
                    .Where(t => t.TransactionType == "Sale" && 
                           t.TransactionDate >= startDate && 
                           t.TransactionDate <= endDate.AddDays(1))
                    .Include(t => t.InventoryItem)
                    .ToListAsync();

                // Calculate summary metrics
                salesReportViewModel.TotalSales = salesTransactions.Sum(t => t.Quantity * (t.UnitPrice ?? 0));
                salesReportViewModel.TotalItemsSold = salesTransactions.Sum(t => t.Quantity);
                salesReportViewModel.AverageOrderValue = salesTransactions.Count > 0 ? 
                    salesReportViewModel.TotalSales / salesTransactions.Select(t => t.ReferenceNumber).Distinct().Count() : 0;
                
                // Group sales by day
                var salesByDate = salesTransactions
                    .GroupBy(t => t.TransactionDate.Date)
                    .Select(g => new DailySalesSummary
                    {
                        Date = g.Key,
                        TotalAmount = g.Sum(t => t.Quantity * (t.UnitPrice ?? 0)),
                        ItemsSold = g.Sum(t => t.Quantity),
                        TransactionCount = g.Select(t => t.ReferenceNumber).Distinct().Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();
                
                salesReportViewModel.DailySales = salesByDate;
                
                // Group sales by item type
                var salesByType = salesTransactions
                    .GroupBy(t => t.InventoryItem.TeaType)
                    .Select(g => new TeaTypeSalesSummary
                    {
                        TeaType = g.Key,
                        TotalAmount = g.Sum(t => t.Quantity * (t.UnitPrice ?? 0)),
                        ItemsSold = g.Sum(t => t.Quantity),
                        Percentage = 0 // Will calculate below
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();
                
                // Calculate percentage for each type
                if (salesReportViewModel.TotalSales > 0)
                {
                    foreach (var type in salesByType)
                    {
                        type.Percentage = (double)((type.TotalAmount / salesReportViewModel.TotalSales) * 100);
                    }
                }
                
                salesReportViewModel.SalesByTeaType = salesByType;
                
                // Top selling products
                var topProducts = salesTransactions
                    .GroupBy(t => new { t.InventoryItemId, t.InventoryItem.Name, t.InventoryItem.TeaType, t.InventoryItem.Grade })
                    .Select(g => new TopSellingProductSummary
                    {
                        ProductId = g.Key.InventoryItemId,
                        ProductName = g.Key.Name,
                        TeaType = g.Key.TeaType,
                        Grade = g.Key.Grade,
                        TotalAmount = g.Sum(t => t.Quantity * (t.UnitPrice ?? 0)),
                        ItemsSold = g.Sum(t => t.Quantity)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(10)
                    .ToList();
                
                salesReportViewModel.TopSellingProducts = topProducts;

                // Generate PDF using the PDF service
                var pdfService = HttpContext.RequestServices.GetRequiredService<PdfService>();
                var fileBytes = await pdfService.GenerateSalesReport(salesReportViewModel);
                var fileName = $"Sales_Report_{DateTime.Now:yyyyMMdd}.pdf";
                
                // Return PDF file
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error exporting sales report: {ex}");
                TempData["InfoMessage"] = $"Error exporting sales report: {ex.Message}";
                
                return RedirectToAction("SalesReport", new { startDate, endDate });
            }
        }

        // Helper method to populate dashboard data
        private void PopulateDashboardData(DashboardViewModel model)
        {
            try 
            {
                // Get deliveries in the selected date range
                var deliveries = _context.Deliveries
                    .Where(d => d.DeliveryDate >= model.StartDate && d.DeliveryDate <= model.EndDate.AddDays(1))
                    .Include(d => d.DeliveryItems)
                    .ThenInclude(di => di.Item)
                    .ToList();

                // Flatten all delivery items for easier aggregation
                var allDeliveryItems = deliveries.SelectMany(d => d.DeliveryItems).ToList();

                // Total 'Sales' = sum of all delivery totals
                model.TotalSales = deliveries.Sum(d => d.TotalAmount ?? 0);
                // Total 'Units Sold' = sum of all item quantities delivered
                model.TotalSalesQuantity = allDeliveryItems.Sum(di => di.Quantity);
                // Production and Delivery quantities (for chart compatibility, set production = 0)
                model.TotalProductionQuantity = 0;
                model.TotalDeliveryQuantity = allDeliveryItems.Sum(di => di.Quantity);

                // Transaction count = number of deliveries
                model.SalesTransactionCount = deliveries.Count;
                // Average order value = average delivery total
                model.AverageOrderValue = deliveries.Count > 0 ? model.TotalSales / deliveries.Count : 0;
                // Average daily sales = total sales / days in range
                model.AverageDailySales = (model.EndDate - model.StartDate).Days + 1 > 0 ? model.TotalSales / ((model.EndDate - model.StartDate).Days + 1) : 0;

                // Chart labels (dates)
                var daysInRange = (model.EndDate - model.StartDate).Days + 1;
                model.ChartLabels = Enumerable.Range(0, daysInRange)
                    .Select(i => model.StartDate.AddDays(i).ToString("MMM dd"))
                    .ToList();

                // Group deliveries by date for chart data
                var deliveriesByDate = deliveries
                    .GroupBy(d => d.DeliveryDate.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.TotalAmount ?? 0));

                // Fill in the sales chart data (really: delivery totals per day)
                model.SalesChartData = model.ChartLabels
                    .Select(label => {
                        var date = DateTime.ParseExact(label, "MMM dd", null);
                        return deliveriesByDate.ContainsKey(date) ? (double)deliveriesByDate[date] : 0;
                    })
                    .ToList();

                // Production chart data is zero (not used)
                model.ProductionChartData = model.ChartLabels.Select(_ => 0.0).ToList();

                // Pie chart: group by item category (or name if no category)
                var itemsByType = allDeliveryItems
                    .GroupBy(di => di.Item != null ? (di.Item.Category ?? di.Item.Name) : "Unknown")
                    .Select(g => new {
                        Type = g.Key,
                        Total = g.Sum(di => di.TotalPrice ?? 0)
                    })
                    .OrderByDescending(x => x.Total)
                    .ToList();
                model.PieChartLabels = itemsByType.Select(x => x.Type).ToList();
                model.PieChartData = itemsByType.Select(x => (double)x.Total).ToList();

                // Top delivered items (by revenue/total price)
                var topItems = allDeliveryItems
                    .GroupBy(di => new {
                        di.ItemId,
                        Name = di.Item != null ? di.Item.Name : "Unknown",
                        TeaType = di.Item != null ? di.Item.Category : "Unknown",
                        Grade = di.Item != null ? di.Item.Unit : "N/A"
                    })
                    .Select(g => new TopSellingItemViewModel {
                        ItemId = g.Key.ItemId,
                        Name = g.Key.Name,
                        TeaType = g.Key.TeaType,
                        Grade = g.Key.Grade,
                        QuantitySold = g.Sum(di => di.Quantity),
                        Revenue = g.Sum(di => di.TotalPrice ?? 0)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();
                model.TopSellingItems = topItems;

                if (!deliveries.Any())
                {
                    TempData["InfoMessage"] = "No delivery data available for the selected period.";
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error populating dashboard data: {ex}");
                // Initialize empty collections
                model.ChartLabels = new List<string>();
                model.SalesChartData = new List<double>();
                model.ProductionChartData = new List<double>();
                model.PieChartLabels = new List<string>();
                model.PieChartData = new List<double>();
                model.TopSellingItems = new List<TopSellingItemViewModel>();
                TempData["InfoMessage"] = $"Error loading dashboard data: {ex.Message}";
            }
        }
    }
} 
