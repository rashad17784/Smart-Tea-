using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TeaOnlineShopContext _context;

        public HomeController(ILogger<HomeController> logger,TeaOnlineShopContext context)
        {
            _logger = logger;
            _context = context ;
        }
        public IActionResult Index()
        {
            var banners = _context.Banners.ToList();
            ViewData["banners"] = banners;
            
            try
            {
                // Ensure we load images properly for display
                var productsWithValidData = _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.ImageName) && 
                           !string.IsNullOrEmpty(p.Title) && 
                           p.Price > 0)
                    .ToList();
                
                if (productsWithValidData.Count == 0)
                {
                    _logger.LogWarning("No valid products found in the database. Check product data.");
                    ViewData["bestSellingProducts"] = new List<Product>();
                    ViewData["newArrivals"] = new List<Product>();
                    return View();
                }
                
                // Get best selling products
                var bestSellingProducts = productsWithValidData
                    .OrderByDescending(p => p.Quantity)
                    .Take(8)
                    .ToList();
                
                // Get new arrivals
                var newArrivals = productsWithValidData
                    .OrderByDescending(p => p.Id)
                    .Take(8)
                    .ToList();
                
                // Log the data
                _logger.LogInformation($"Retrieved {bestSellingProducts.Count} best selling products and {newArrivals.Count} new arrivals.");
                
                ViewData["bestSellingProducts"] = bestSellingProducts;
                ViewData["newArrivals"] = newArrivals;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading products: {ex.Message}");
                ViewData["bestSellingProducts"] = new List<Product>();
                ViewData["newArrivals"] = new List<Product>();
            }
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View("About");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
