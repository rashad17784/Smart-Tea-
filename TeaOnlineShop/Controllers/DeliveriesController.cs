using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;
using System.Security.Claims;

namespace TeaOnlineShop.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = TeaOnlineShop.Authorization.AppRoles.Administrator)]
    public class DeliveriesController : Controller
    {
        private readonly TeaOnlineShopContext _context;
        private readonly StockLedgerService _stockLedger;
        private readonly QrAuditService _qrAuditService;

        public DeliveriesController(
            TeaOnlineShopContext context,
            StockLedgerService stockLedger,
            QrAuditService qrAuditService)
        {
            _context = context;
            _stockLedger = stockLedger;
            _qrAuditService = qrAuditService;
        }

        // GET: Deliveries
        public async Task<IActionResult> Index()
        {
            try
            {
                // Check if table exists first
                var connection = _context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Delivery'";
                var tableExists = (int)command.ExecuteScalar() > 0;
                
                if (!tableExists)
                {
                    TempData["ErrorMessage"] = "Delivery schema is unavailable. Apply the approved database migrations before using this module.";
                    return View(new List<Delivery>());
                }
                
                var deliveries = await _context.Deliveries
                    .Include(d => d.Supplier)
                    .OrderByDescending(d => d.DeliveryDate)
                    .ToListAsync();
                    
                return View(deliveries);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error fetching deliveries: {ex.Message}";
                return View(new List<Delivery>());
            }
        }

        // GET: Deliveries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var delivery = await _context.Deliveries
                .Include(d => d.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (delivery == null)
            {
                return NotFound();
            }

            // Get delivery items
            var items = await _context.DeliveryItems
                .Include(i => i.Item)
                .Where(i => i.DeliveryId == id)
                .ToListAsync();
                
            ViewBag.DeliveryItems = items;

            return View(delivery);
        }

        // GET: Deliveries/Create
        public async Task<IActionResult> Create(int? supplierId = null)
        {
            try
            {
                var viewModel = new DeliveryViewModel
                {
                    DeliveryCode = await GenerateDeliveryCode(),
                    DeliveryDate = DateTime.Now,
                    Status = "Received",
                    SuppliersList = await GetSuppliersSelectList(),
                    SupplyItemsList = await GetSupplyItemsSelectList(),
                    Items = new List<DeliveryItemViewModel>
                    {
                        new DeliveryItemViewModel() // Start with one item
                    }
                };
                
                if (supplierId.HasValue)
                {
                    viewModel.SupplierId = supplierId.Value;
                    var supplier = await _context.Suppliers.FindAsync(supplierId.Value);
                    if (supplier != null)
                    {
                        viewModel.SupplierName = supplier.Name;
                    }
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error preparing delivery form: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Deliveries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryViewModel viewModel)
        {
            // Validate that there's at least one item
            if (viewModel.Items == null || !viewModel.Items.Any(i => i.ItemId > 0 && i.Quantity > 0))
            {
                ModelState.AddModelError("", "At least one delivery item is required");
            }
            
            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);
                // Get current user ID
                int receivedById = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
                    ? actorId
                    : throw new InvalidOperationException("The authenticated receiver has no valid user identifier.");

                var delivery = new Delivery
                {
                    DeliveryCode = viewModel.DeliveryCode,
                    SupplierId = viewModel.SupplierId,
                    ReceivedById = receivedById,
                    ReceivedByName = User.Identity?.Name ?? "Administrator",
                    DeliveryDate = viewModel.DeliveryDate,
                    Status = viewModel.Status,
                    Notes = viewModel.Notes,
                    TotalAmount = 0 // Will be calculated below
                };
                
                _context.Add(delivery);
                await _context.SaveChangesAsync();
                
                // Process delivery items
                decimal totalAmount = 0;
                foreach (var itemVM in viewModel.Items.Where(i => i.ItemId > 0 && i.Quantity > 0).OrderBy(i => i.ItemId))
                {
                    // Calculate total price if needed
                    if (itemVM.UnitPrice.HasValue && itemVM.UnitPrice > 0)
                    {
                        itemVM.TotalPrice = itemVM.Quantity * itemVM.UnitPrice;
                        totalAmount += itemVM.TotalPrice.Value;
                    }
                    
                    var deliveryItem = new DeliveryItem
                    {
                        DeliveryId = delivery.Id,
                        ItemId = itemVM.ItemId,
                        Quantity = itemVM.Quantity,
                        UnitPrice = itemVM.UnitPrice,
                        TotalPrice = itemVM.TotalPrice,
                        Notes = itemVM.Notes
                    };
                    
                    _context.Add(deliveryItem);
                    
                    var supplyItem = await _context.SupplyItems.FindAsync(itemVM.ItemId);
                    if (supplyItem == null)
                    {
                        throw new InvalidOperationException($"Supply item {itemVM.ItemId} no longer exists.");
                    }
                    if (string.Equals(delivery.Status, "Received", StringComparison.OrdinalIgnoreCase))
                    {
                        await _stockLedger.RecordSupplyMovementAsync(supplyItem.Id, new StockMovementRequest
                        {
                            MovementType = "SupplierReceipt",
                            QuantityChange = itemVM.Quantity,
                            ReferenceType = "Delivery",
                            ReferenceId = delivery.Id,
                            ReferenceNumber = delivery.DeliveryCode,
                            Reason = string.IsNullOrWhiteSpace(itemVM.Notes)
                                ? $"Goods received on delivery {delivery.DeliveryCode}."
                                : itemVM.Notes,
                            PerformedByUserId = receivedById,
                            PerformedByName = delivery.ReceivedByName,
                            UnitCost = itemVM.UnitPrice
                        });
                    }
                }
                
                // Update delivery total amount
                delivery.TotalAmount = totalAmount;
                _context.Update(delivery);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return RedirectToAction(nameof(Details), new { id = delivery.Id });
            }
            
            // If we get here, something failed. Redisplay form
            viewModel.SuppliersList = await GetSuppliersSelectList();
            viewModel.SupplyItemsList = await GetSupplyItemsSelectList();
            
            return View(viewModel);
        }

        // GET: Deliveries/ScanSupplier
        public IActionResult ScanSupplier()
        {
            return View(new QRScanViewModel());
        }

        // Ajax endpoint to get supplier info from QR code
        [HttpPost]
        public async Task<IActionResult> GetSupplierFromQR(string qrCodeData)
        {
            if (string.IsNullOrEmpty(qrCodeData))
            {
                return Json(new { success = false, message = "QR code data is required" });
            }
            
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.QRCodeData == qrCodeData);
                
            if (supplier != null)
            {
                await _qrAuditService.RecordAsync(qrCodeData, "Supplier", supplier.Id, true,
                    "Found", "DeliverySupplierLookup", $"Supplier selected for receipt: {supplier.Name}");
                
                return Json(new
                {
                    success = true,
                    supplier = new
                    {
                        id = supplier.Id,
                        name = supplier.Name,
                        code = supplier.SupplierCode,
                        contactPerson = supplier.ContactPerson,
                        phone = supplier.Phone,
                        email = supplier.Email,
                        status = supplier.Status
                    }
                });
            }
            else
            {
                await _qrAuditService.RecordAsync(qrCodeData, "Supplier", null, false,
                    "NotFound", "DeliverySupplierLookup", "No supplier matched the scanned code.");
                
                return Json(new 
                { 
                    success = false, 
                    message = "No supplier found with this QR code",
                    qrCodeData = qrCodeData
                });
            }
        }

        // GET: Deliveries/SupplyItems
        public async Task<IActionResult> SupplyItems()
        {
            var items = await _context.SupplyItems
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToListAsync();
                
            return View(items);
        }

        // GET: Deliveries/CreateSupplyItem
        public IActionResult CreateSupplyItem()
        {
            return View();
        }

        // POST: Deliveries/CreateSupplyItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplyItem(SupplyItem item)
        {
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(SupplyItems));
            }
            
            return View(item);
        }

        // Helper methods
        private async Task<string> GenerateDeliveryCode()
        {
            try
            {
                string dateCode = DateTime.Now.ToString("yyyyMMdd");
                
                // Check if the table exists first
                var connection = _context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Delivery'";
                var tableExists = (int)command.ExecuteScalar() > 0;
                
                if (!tableExists)
                {
                    throw new InvalidOperationException("Delivery schema is unavailable. Apply database migrations.");
                }
                
                // If the table exists, count deliveries with the same date code
                int deliveryCount = await _context.Deliveries
                    .CountAsync(d => d.DeliveryCode.StartsWith($"DEL-{dateCode}"));
                    
                return $"DEL-{dateCode}-{(deliveryCount + 1):000}";
            }
            catch (Exception ex)
            {
                // In case of error, return a safe default
                return $"DEL-{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            }
        }
        
        private async Task<List<SelectListItem>> GetSuppliersSelectList()
        {
            try
            {
                // Check if Supplier table exists
                var connection = _context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Supplier'";
                var tableExists = (int)command.ExecuteScalar() > 0;
                
                if (!tableExists)
                {
                    // Call supplier controller's table creation method (placeholder message here)
                    TempData["ErrorMessage"] = "Supplier table not found. Please add suppliers first.";
                    return new List<SelectListItem>();
                }
                
                var suppliers = await _context.Suppliers
                    .Where(s => s.Status == "Active")
                    .OrderBy(s => s.Name)
                    .ToListAsync();
                    
                return suppliers.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Name} ({s.SupplierCode})"
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSuppliersSelectList: {ex.Message}");
                // Return an empty list in case of error
                return new List<SelectListItem>();
            }
        }
        
        private async Task<List<SelectListItem>> GetSupplyItemsSelectList()
        {
            try
            {
                // Check if SupplyItem table exists
                var connection = _context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SupplyItem'";
                var tableExists = (int)command.ExecuteScalar() > 0;
                
                if (!tableExists)
                {
                    TempData["ErrorMessage"] = "Supply item schema is unavailable. Apply the approved database migrations.";
                    return new List<SelectListItem>();
                }
                
                var items = await _context.SupplyItems
                    .OrderBy(i => i.Category)
                    .ThenBy(i => i.Name)
                    .ToListAsync();
                    
                return items.Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Name} ({i.Category}) - {i.Unit}"
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSupplyItemsSelectList: {ex.Message}");
                // Return an empty list in case of error
                return new List<SelectListItem>();
            }
        }
        
        // Ajax endpoint to get item details
        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int id)
        {
            var item = await _context.SupplyItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            
            return Json(new
            {
                id = item.Id,
                name = item.Name,
                category = item.Category,
                unit = item.Unit,
                currentStock = item.CurrentStock
            });
        }
        
        // Ajax endpoint to add delivery item row
        [HttpGet]
        public async Task<IActionResult> GetItemRow(int index)
        {
            var items = await GetSupplyItemsSelectList();
            
            return PartialView("_DeliveryItemRow", new
            {
                Index = index,
                Items = items
            });
        }
    }
} 
