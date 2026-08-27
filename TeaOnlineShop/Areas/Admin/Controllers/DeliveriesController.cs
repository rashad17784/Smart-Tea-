using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Services;
using System.Security.Claims;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AppPermissions.InventoryView)]
    public class DeliveriesController : AdminBaseController
    {
        private readonly TeaOnlineShopContext _context;
        private readonly StockLedgerService _stockLedger;

        public DeliveriesController(TeaOnlineShopContext context, StockLedgerService stockLedger)
        {
            _context = context;
            _stockLedger = stockLedger;
        }

        // GET: Admin/Deliveries
        public async Task<IActionResult> Index()
        {
            try
            {
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

        // GET: Admin/Deliveries/Details/5
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

        // GET: Admin/Deliveries/Create
        [Authorize(Policy = AppPermissions.InventoryReceive)]
        public async Task<IActionResult> Create(int? supplierId = null, string supplierCode = null)
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
                else if (!string.IsNullOrEmpty(supplierCode))
                {
                    // Find supplier by supplier code
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.SupplierCode == supplierCode);
                        
                    if (supplier != null)
                    {
                        viewModel.SupplierId = supplier.Id;
                        viewModel.SupplierName = supplier.Name;
                    }
                    else
                    {
                        TempData["InfoMessage"] = $"No supplier found with code: {supplierCode}";
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

        // POST: Admin/Deliveries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryReceive)]
        public async Task<IActionResult> Create(DeliveryViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable);
                    var includeFinancialData = User.HasClaim(
                        AppPermissions.ClaimType,
                        AppPermissions.DashboardFinancialView);
                    // Create delivery
                    var delivery = new Delivery
                    {
                        DeliveryCode = viewModel.DeliveryCode,
                        SupplierId = viewModel.SupplierId,
                        ReceivedById = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                            ? userId
                            : 0,
                        ReceivedByName = User.Identity?.Name ?? "Unknown staff user",
                        DeliveryDate = viewModel.DeliveryDate,
                        Status = viewModel.Status,
                        Notes = viewModel.Notes,
                        TotalAmount = includeFinancialData
                            ? viewModel.Items.Sum(i => i.TotalPrice)
                            : null
                    };
                    
                    _context.Deliveries.Add(delivery);
                    await _context.SaveChangesAsync();
                    
                    // Create delivery items
                    foreach (var itemViewModel in viewModel.Items.OrderBy(x => x.ItemId))
                    {
                        if (itemViewModel.ItemId > 0 && itemViewModel.Quantity > 0)
                        {
                            var item = new DeliveryItem
                            {
                                DeliveryId = delivery.Id,
                                ItemId = itemViewModel.ItemId,
                                Quantity = itemViewModel.Quantity,
                                UnitPrice = includeFinancialData ? itemViewModel.UnitPrice : null,
                                TotalPrice = includeFinancialData ? itemViewModel.TotalPrice : null,
                                Notes = itemViewModel.Notes
                            };
                            
                            _context.DeliveryItems.Add(item);

                            var supplyItem = await _context.SupplyItems.FindAsync(itemViewModel.ItemId);
                            if (supplyItem == null)
                            {
                                throw new InvalidOperationException($"Supply item {itemViewModel.ItemId} no longer exists.");
                            }

                            if (string.Equals(delivery.Status, "Received", StringComparison.OrdinalIgnoreCase))
                            {
                                await _stockLedger.RecordSupplyMovementAsync(supplyItem.Id, new StockMovementRequest
                                {
                                    MovementType = "SupplierReceipt",
                                    QuantityChange = itemViewModel.Quantity,
                                    ReferenceType = "Delivery",
                                    ReferenceId = delivery.Id,
                                    ReferenceNumber = delivery.DeliveryCode,
                                    Reason = string.IsNullOrWhiteSpace(itemViewModel.Notes)
                                        ? $"Goods received on delivery {delivery.DeliveryCode}."
                                        : itemViewModel.Notes,
                                    PerformedByUserId = delivery.ReceivedById > 0 ? delivery.ReceivedById : null,
                                    PerformedByName = delivery.ReceivedByName,
                                    UnitCost = includeFinancialData ? itemViewModel.UnitPrice : null
                                });
                            }
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    TempData["Message"] = $"Delivery {delivery.DeliveryCode} recorded successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating delivery: {ex.Message}");
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.SuppliersList = await GetSuppliersSelectList();
            viewModel.SupplyItemsList = await GetSupplyItemsSelectList();
            return View(viewModel);
        }

        // GET: Admin/Deliveries/SupplyItems
        public async Task<IActionResult> SupplyItems()
        {
            var items = await _context.SupplyItems.OrderBy(i => i.Category).ThenBy(i => i.Name).ToListAsync();
            return View(items);
        }

        // GET: Admin/Deliveries/CreateSupplyItem
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public IActionResult CreateSupplyItem()
        {
            return View();
        }

        // POST: Admin/Deliveries/CreateSupplyItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public async Task<IActionResult> CreateSupplyItem([Bind("Id,Name,Category,Unit,Description,MinimumStock,CurrentStock")] SupplyItem supplyItem)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    var openingQuantity = supplyItem.CurrentStock;
                    supplyItem.CurrentStock = 0;
                    supplyItem.ItemCode = $"PENDING-{Guid.NewGuid():N}";
                    _context.Add(supplyItem);
                    await _context.SaveChangesAsync();
                    supplyItem.ItemCode = $"SUPITEM-{supplyItem.Id}";
                    await _context.SaveChangesAsync();

                    if (openingQuantity > 0)
                    {
                        await _stockLedger.RecordSupplyMovementAsync(supplyItem.Id, new StockMovementRequest
                        {
                            MovementType = "OpeningBalance",
                            QuantityChange = openingQuantity,
                            ReferenceType = "InventoryOnboarding",
                            ReferenceId = supplyItem.Id,
                            ReferenceNumber = supplyItem.ItemCode,
                            Reason = "Opening balance recorded when the supply item was created.",
                            PerformedByUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
                                ? actorId
                                : null,
                            PerformedByName = User.Identity?.Name ?? "Unknown staff user"
                        });
                    }

                    await transaction.CommitAsync();
                    TempData["Message"] = "Supply item created successfully";
                    return RedirectToAction(nameof(SupplyItems));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating supply item: {ex.Message}");
                }
            }
            
            return View(supplyItem);
        }

        // GET: Admin/Deliveries/EditSupplyItem/5
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public async Task<IActionResult> EditSupplyItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplyItem = await _context.SupplyItems.FindAsync(id);
            if (supplyItem == null)
            {
                return NotFound();
            }
            
            return View(supplyItem);
        }

        // POST: Admin/Deliveries/EditSupplyItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public async Task<IActionResult> EditSupplyItem(int id, [Bind("Id,Name,Category,Unit,Description,MinimumStock,CurrentStock")] SupplyItem supplyItem)
        {
            if (id != supplyItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.SupplyItems.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.Name = supplyItem.Name;
                    existing.Category = supplyItem.Category;
                    existing.Unit = supplyItem.Unit;
                    existing.Description = supplyItem.Description;
                    existing.MinimumStock = supplyItem.MinimumStock;
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Supply item updated successfully";
                    return RedirectToAction(nameof(SupplyItems));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplyItemExists(supplyItem.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating supply item: {ex.Message}");
                }
            }
            
            return View(supplyItem);
        }

        // GET: Admin/Deliveries/ScanSupplier
        public IActionResult ScanSupplier()
        {
            return View();
        }

        // Helper methods
        private async Task<string> GenerateDeliveryCode()
        {
            try
            {
                string dateCode = DateTime.Now.ToString("yyyyMMdd");
                
                // Count deliveries with the same date code
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
            var suppliers = await _context.Suppliers
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Name} ({s.SupplierCode})"
                })
                .ToListAsync();
                
            return suppliers;
        }
        
        private async Task<List<SelectListItem>> GetSupplyItemsSelectList()
        {
            var items = await _context.SupplyItems
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.ItemCode} · {i.Name} ({i.Category}) - {i.Unit}"
                })
                .ToListAsync();
                
            return items;
        }
        
        // AJAX endpoints for the delivery form
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
                category = item.Category,
                unit = item.Unit,
                currentStock = item.CurrentStock
            });
        }
        
        [HttpGet]
        public IActionResult GetItemRow(int index)
        {
            return PartialView("_DeliveryItemRow", index);
        }

        private bool SupplyItemExists(int id)
        {
            return _context.SupplyItems.Any(e => e.Id == id);
        }
    }
} 
