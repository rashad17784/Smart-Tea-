using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TeaOnlineShop.Authorization;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    [Authorize(Policy = AppPermissions.InventoryView)]
    public class TeaInventoryController : AdminBaseController
    {
        private readonly TeaOnlineShopContext _context;
        private readonly QRCodeService _qrCodeService;
        private readonly InventoryService _inventoryService;
        private readonly QrAuditService _qrAuditService;
        private readonly WarehousePermissionOptions _warehouseOptions;

        public TeaInventoryController(
            TeaOnlineShopContext context, 
            QRCodeService qrCodeService,
            InventoryService inventoryService,
            QrAuditService qrAuditService,
            IOptions<WarehousePermissionOptions> warehouseOptions)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _inventoryService = inventoryService;
            _qrAuditService = qrAuditService;
            _warehouseOptions = warehouseOptions.Value;
        }

        // GET: Admin/TeaInventory
        public async Task<IActionResult> Index()
        {
            try
            {
                // Use projection to safely handle potential NULL values in the database
                var items = await _context.TeaInventoryItems
                    .Select(i => new TeaInventoryItem
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode,
                        Name = i.Name ?? string.Empty,
                        TeaType = i.TeaType ?? string.Empty,
                        Grade = i.Grade ?? string.Empty,
                        Origin = i.Origin ?? string.Empty,
                        HarvestSeason = i.HarvestSeason ?? string.Empty,
                        BatchNumber = i.BatchNumber ?? string.Empty,
                        Description = i.Description ?? string.Empty,
                        Unit = i.Unit ?? string.Empty,
                        Status = i.Status ?? string.Empty,
                        QRCodeData = i.QRCodeData ?? string.Empty,
                        CurrentStock = i.CurrentStock,
                        MinimumStock = i.MinimumStock,
                        ReorderLevel = i.ReorderLevel,
                        ReorderQuantity = i.ReorderQuantity,
                        UnitCost = i.UnitCost,
                        RetailPrice = i.RetailPrice,
                        CreatedDate = i.CreatedDate,
                        LastUpdated = i.LastUpdated,
                        HarvestDate = i.HarvestDate,
                        HasBeenCorrected = i.HasBeenCorrected,
                        LastCorrectionDate = i.LastCorrectionDate,
                        LastCorrectedBy = i.LastCorrectedBy ?? string.Empty,
                        CorrectionReason = i.CorrectionReason ?? string.Empty
                    })
                    .OrderBy(i => i.TeaType)
                    .ThenBy(i => i.Grade)
                    .ToListAsync();
                
                return View(items);
            }
            catch (Exception ex)
            {
                // Log the full exception details for debugging
                System.Diagnostics.Debug.WriteLine($"Error retrieving inventory items: {ex}");
                TempData["ErrorMessage"] = $"Error retrieving inventory items: {ex.Message}";
                return View(new List<TeaInventoryItem>());
            }
        }

        // GET: Admin/TeaInventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try 
            {
                var item = await _context.TeaInventoryItems
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (item == null)
                {
                    return NotFound();
                }
                
                // Ensure no null string values exist
                item.Name ??= string.Empty;
                item.TeaType ??= string.Empty;
                item.Grade ??= string.Empty;
                item.Origin ??= string.Empty;
                item.HarvestSeason ??= string.Empty;
                item.BatchNumber ??= string.Empty;
                item.Description ??= string.Empty;
                item.Unit ??= string.Empty;
                item.Status ??= string.Empty;
                item.QRCodeData ??= string.Empty;
                item.LastCorrectedBy ??= string.Empty;
                item.CorrectionReason ??= string.Empty;
                
                // Get transaction history
                ViewBag.Transactions = await _inventoryService.GetTransactionHistoryAsync(item.Id, 15);

                return View(item);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving inventory item details: {ex}");
                TempData["ErrorMessage"] = $"Error retrieving inventory item details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/TeaInventory/Create
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public IActionResult Create()
        {
            // Prepare lists for dropdowns
            ViewBag.TeaTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Black", Text = "Black Tea" },
                new SelectListItem { Value = "Green", Text = "Green Tea" },
                new SelectListItem { Value = "White", Text = "White Tea" },
                new SelectListItem { Value = "Oolong", Text = "Oolong Tea" },
                new SelectListItem { Value = "Herbal", Text = "Herbal Tea" },
                new SelectListItem { Value = "Pu-erh", Text = "Pu-erh Tea" }
            };
            
            ViewBag.Grades = new List<SelectListItem>
            {
                new SelectListItem { Value = "BOP", Text = "BOP - Broken Orange Pekoe" },
                new SelectListItem { Value = "BOPF", Text = "BOPF - Broken Orange Pekoe Fannings" },
                new SelectListItem { Value = "DUST", Text = "DUST" },
                new SelectListItem { Value = "FNGS", Text = "FNGS - Fannings" },
                new SelectListItem { Value = "OP", Text = "OP - Orange Pekoe" },
                new SelectListItem { Value = "Premium", Text = "Premium" },
                new SelectListItem { Value = "AAA", Text = "AAA" },
                new SelectListItem { Value = "AA", Text = "AA" },
                new SelectListItem { Value = "A", Text = "A" },
                new SelectListItem { Value = "B", Text = "B" },
                new SelectListItem { Value = "C", Text = "C" }
            };
            
            return View(new TeaInventoryCreateViewModel());
        }
        
        // POST: Admin/TeaInventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public async Task<IActionResult> Create(TeaInventoryCreateViewModel model)
        {
            model.ItemCode = (model.ItemCode ?? string.Empty).Trim().ToUpperInvariant();

            if (ModelState.IsValid && await _context.TeaInventoryItems
                    .AnyAsync(x => x.ItemCode == model.ItemCode))
            {
                ModelState.AddModelError(nameof(model.ItemCode),
                    "This item code is already in use. Item codes must be unique.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var entity = new TeaInventoryItem
                    {
                        ItemCode = model.ItemCode,
                        Name = (model.Name ?? string.Empty).Trim(),
                        TeaType = model.TeaType,
                        Grade = model.Grade,
                        Unit = model.Unit,
                        Origin = (model.Origin ?? string.Empty).Trim(),
                        HarvestSeason = (model.HarvestSeason ?? string.Empty).Trim(),
                        HarvestDate = model.HarvestDate,
                        Description = (model.Description ?? string.Empty).Trim(),
                        BatchNumber = (model.BatchNumber ?? string.Empty).Trim(),
                        CurrentStock = model.InitialStock,
                        MinimumStock = model.MinimumStock,
                        ReorderLevel = model.ReorderLevel,
                        ReorderQuantity = model.ReorderQuantity,
                        UnitCost = model.UnitCost,
                        RetailPrice = model.RetailPrice,
                        Status = model.Status
                    };

                    var item = await _inventoryService.CreateInventoryItemAsync(entity, User.Identity?.Name ?? "System");
                    
                    TempData["SuccessMessage"] = "Inventory item created successfully";
                    return RedirectToAction(nameof(Details), new { id = item.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty,
                        "The inventory item could not be created. Verify the item code and try again.");
                    System.Diagnostics.Debug.WriteLine($"Exception in Create: {ex}");
                }
            }
            
            // Repopulate dropdown lists in case of error
            ViewBag.TeaTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Black", Text = "Black Tea" },
                new SelectListItem { Value = "Green", Text = "Green Tea" },
                new SelectListItem { Value = "White", Text = "White Tea" },
                new SelectListItem { Value = "Oolong", Text = "Oolong Tea" },
                new SelectListItem { Value = "Herbal", Text = "Herbal Tea" },
                new SelectListItem { Value = "Pu-erh", Text = "Pu-erh Tea" }
            };
            
            ViewBag.Grades = new List<SelectListItem>
            {
                new SelectListItem { Value = "BOP", Text = "BOP - Broken Orange Pekoe" },
                new SelectListItem { Value = "BOPF", Text = "BOPF - Broken Orange Pekoe Fannings" },
                new SelectListItem { Value = "DUST", Text = "DUST" },
                new SelectListItem { Value = "FNGS", Text = "FNGS - Fannings" },
                new SelectListItem { Value = "OP", Text = "OP - Orange Pekoe" },
                new SelectListItem { Value = "Premium", Text = "Premium" },
                new SelectListItem { Value = "AAA", Text = "AAA" },
                new SelectListItem { Value = "AA", Text = "AA" },
                new SelectListItem { Value = "A", Text = "A" },
                new SelectListItem { Value = "B", Text = "B" },
                new SelectListItem { Value = "C", Text = "C" }
            };
            
            return View(model);
        }

        // GET: Admin/TeaInventory/Edit/5
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.TeaInventoryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            
            // Prepare lists for dropdowns
            ViewBag.TeaTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Black", Text = "Black Tea" },
                new SelectListItem { Value = "Green", Text = "Green Tea" },
                new SelectListItem { Value = "White", Text = "White Tea" },
                new SelectListItem { Value = "Oolong", Text = "Oolong Tea" },
                new SelectListItem { Value = "Herbal", Text = "Herbal Tea" },
                new SelectListItem { Value = "Pu-erh", Text = "Pu-erh Tea" }
            };
            
            ViewBag.Grades = new List<SelectListItem>
            {
                new SelectListItem { Value = "BOP", Text = "BOP - Broken Orange Pekoe" },
                new SelectListItem { Value = "BOPF", Text = "BOPF - Broken Orange Pekoe Fannings" },
                new SelectListItem { Value = "DUST", Text = "DUST" },
                new SelectListItem { Value = "FNGS", Text = "FNGS - Fannings" },
                new SelectListItem { Value = "OP", Text = "OP - Orange Pekoe" },
                new SelectListItem { Value = "Premium", Text = "Premium" },
                new SelectListItem { Value = "AAA", Text = "AAA" },
                new SelectListItem { Value = "AA", Text = "AA" },
                new SelectListItem { Value = "A", Text = "A" },
                new SelectListItem { Value = "B", Text = "B" },
                new SelectListItem { Value = "C", Text = "C" }
            };
            
            return View(item);
        }

        // POST: Admin/TeaInventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
        public async Task<IActionResult> Edit(int id, TeaInventoryItem model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var item = await _context.TeaInventoryItems.FindAsync(id);
                    if (item == null)
                    {
                        return NotFound();
                    }
                    
                    // Update properties but preserve stock levels
                    var currentStock = item.CurrentStock;
                    
                    item.Name = model.Name;
                    item.TeaType = model.TeaType;
                    item.Grade = model.Grade;
                    item.Unit = model.Unit;
                    item.Origin = model.Origin;
                    item.HarvestSeason = model.HarvestSeason;
                    item.HarvestDate = model.HarvestDate;
                    item.Description = model.Description;
                    item.BatchNumber = model.BatchNumber;
                    item.MinimumStock = model.MinimumStock;
                    item.ReorderLevel = model.ReorderLevel;
                    item.ReorderQuantity = model.ReorderQuantity;
                    item.UnitCost = model.UnitCost;
                    item.RetailPrice = model.RetailPrice;
                    item.Status = model.Status;
                    item.LastUpdated = DateTime.Now;
                    
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Inventory item updated successfully";
                    return RedirectToAction(nameof(Details), new { id = item.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TeaInventoryItems.Any(e => e.Id == id))
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
                    ModelState.AddModelError("", $"Error updating inventory item: {ex.Message}");
                }
            }
            
            // Repopulate dropdown lists in case of error
            ViewBag.TeaTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Black", Text = "Black Tea" },
                new SelectListItem { Value = "Green", Text = "Green Tea" },
                new SelectListItem { Value = "White", Text = "White Tea" },
                new SelectListItem { Value = "Oolong", Text = "Oolong Tea" },
                new SelectListItem { Value = "Herbal", Text = "Herbal Tea" },
                new SelectListItem { Value = "Pu-erh", Text = "Pu-erh Tea" }
            };
            
            ViewBag.Grades = new List<SelectListItem>
            {
                new SelectListItem { Value = "BOP", Text = "BOP - Broken Orange Pekoe" },
                new SelectListItem { Value = "BOPF", Text = "BOPF - Broken Orange Pekoe Fannings" },
                new SelectListItem { Value = "DUST", Text = "DUST" },
                new SelectListItem { Value = "FNGS", Text = "FNGS - Fannings" },
                new SelectListItem { Value = "OP", Text = "OP - Orange Pekoe" },
                new SelectListItem { Value = "Premium", Text = "Premium" },
                new SelectListItem { Value = "AAA", Text = "AAA" },
                new SelectListItem { Value = "AA", Text = "AA" },
                new SelectListItem { Value = "A", Text = "A" },
                new SelectListItem { Value = "B", Text = "B" },
                new SelectListItem { Value = "C", Text = "C" }
            };
            
            return View(model);
        }

        // GET: Admin/TeaInventory/AdjustStock/5
        [Authorize(Policy = AppPermissions.InventoryAdjustSmall)]
        public async Task<IActionResult> AdjustStock(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.TeaInventoryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            
            var model = new StockAdjustmentViewModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                TeaType = item.TeaType,
                Grade = item.Grade,
                CurrentStock = item.CurrentStock,
                NewStock = item.CurrentStock
            };
            
            return View(model);
        }

        // POST: Admin/TeaInventory/AdjustStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryAdjustSmall)]
        public async Task<IActionResult> AdjustStock(StockAdjustmentViewModel model)
        {
            var currentItem = await _context.TeaInventoryItems.FindAsync(model.ItemId);
            if (currentItem == null)
            {
                return NotFound();
            }

            model.CurrentStock = currentItem.CurrentStock;
            if (User.IsInRole(AppRoles.WarehouseStaff))
            {
                var allowedChange = WarehouseAdjustmentPolicy.MaximumAllowedChange(
                    currentItem.CurrentStock,
                    _warehouseOptions);
                var requestedChange = Math.Abs(model.NewStock - currentItem.CurrentStock);

                if (requestedChange > allowedChange)
                {
                    ModelState.AddModelError(nameof(model.NewStock),
                        $"Warehouse staff may adjust at most {allowedChange:0.##} {currentItem.Unit} " +
                        $"for this item. A manager must authorize larger corrections.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _inventoryService.AdjustStockAsync(
                        model.ItemId, 
                        model.NewStock, 
                        model.Reason, 
                        User.Identity?.Name ?? "System");
                        
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Stock adjusted successfully";
                        return RedirectToAction(nameof(Details), new { id = model.ItemId });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to adjust stock");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error adjusting stock: {ex.Message}");
                }
            }
            
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: Admin/TeaInventory/Scan
        public IActionResult Scan()
        {
            return View();
        }

        // GET: Admin/TeaInventory/FindByQRCode
        [HttpGet]
        public async Task<IActionResult> FindItemByQRCode(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode))
            {
                TempData["ErrorMessage"] = "QR code is required";
                return RedirectToAction(nameof(Scan));
            }

            try
            {
                var item = await _inventoryService.GetItemByQRCodeAsync(qrCode);
                
                if (item == null)
                {
                    await _qrAuditService.RecordAsync(qrCode, "TeaInventoryItem", null, false,
                        "NotFound", "Lookup", "No inventory item matched the scanned QR code.");
                    TempData["ErrorMessage"] = "No inventory item found with this QR code";
                    return RedirectToAction(nameof(Scan));
                }
                
                await _qrAuditService.RecordAsync(qrCode, "TeaInventoryItem", item.Id, true,
                    "Found", "Lookup");
                return RedirectToAction(nameof(Details), new { id = item.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error scanning QR code: {ex.Message}";
                return RedirectToAction(nameof(Scan));
            }
        }

        // POST: Admin/TeaInventory/FindByQRCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindByQRCode(string qrCode)
        {
            if (string.IsNullOrEmpty(qrCode))
            {
                TempData["ErrorMessage"] = "QR code is required";
                return RedirectToAction(nameof(Scan));
            }

            try
            {
                var item = await _inventoryService.GetItemByQRCodeAsync(qrCode);
                
                if (item == null)
                {
                    await _qrAuditService.RecordAsync(qrCode, "TeaInventoryItem", null, false,
                        "NotFound", "Lookup", "No inventory item matched the scanned QR code.");
                    TempData["ErrorMessage"] = "No inventory item found with this QR code";
                    return RedirectToAction(nameof(Scan));
                }
                
                await _qrAuditService.RecordAsync(qrCode, "TeaInventoryItem", item.Id, true,
                    "Found", "Lookup");
                return RedirectToAction(nameof(Details), new { id = item.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error scanning QR code: {ex.Message}";
                return RedirectToAction(nameof(Scan));
            }
        }

        // GET: Admin/TeaInventory/AddStock/5
        [Authorize(Policy = AppPermissions.InventoryTransact)]
        public async Task<IActionResult> AddStock(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.TeaInventoryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            
            var model = new StockTransactionViewModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                TeaType = item.TeaType,
                Grade = item.Grade,
                CurrentStock = item.CurrentStock,
                TransactionType = "Delivery",
                Quantity = 0
            };
            
            ViewBag.TransactionTypes = GetStockInTransactionTypes();
            
            return View(model);
        }

        // POST: Admin/TeaInventory/AddStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryTransact)]
        public async Task<IActionResult> AddStock(StockTransactionViewModel model)
        {
            if (User.IsInRole(AppRoles.WarehouseStaff))
            {
                // Financial values are outside the warehouse role and must be
                // discarded server-side even if a crafted request includes one.
                model.UnitPrice = null;

                // Corrections must go through AdjustStock so the configured
                // unit/percentage cap and mandatory reason are always enforced.
                var permittedTypes = new[] { "Delivery", "Production", "Return" };
                if (!permittedTypes.Contains(model.TransactionType, StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(model.TransactionType),
                        "This stock-in operation is not permitted for Warehouse Staff. Corrections must use Adjust Stock.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _inventoryService.UpdateStockAsync(
                        model.ItemId,
                        model.Quantity,
                        model.TransactionType,
                        User.Identity?.Name ?? "System",
                        model.Notes,
                        null,
                        model.ReferenceNumber,
                        model.UnitPrice);
                        
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Stock added successfully";
                        return RedirectToAction(nameof(Details), new { id = model.ItemId });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to add stock");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error adding stock: {ex.Message}");
                }
            }
            
            // If we got this far, something failed, redisplay form
            var item = await _context.TeaInventoryItems.FindAsync(model.ItemId);
            if (item != null)
            {
                model.CurrentStock = item.CurrentStock;
            }
            
            ViewBag.TransactionTypes = GetStockInTransactionTypes();
            
            return View(model);
        }

        // GET: Admin/TeaInventory/RemoveStock/5
        [Authorize(Policy = AppPermissions.InventoryTransact)]
        public async Task<IActionResult> RemoveStock(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.TeaInventoryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            
            var model = new StockTransactionViewModel
            {
                ItemId = item.Id,
                ItemName = item.Name,
                TeaType = item.TeaType,
                Grade = item.Grade,
                CurrentStock = item.CurrentStock,
                TransactionType = "Sale",
                Quantity = 0
            };
            
            ViewBag.TransactionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Sale", Text = "Sale" },
                new SelectListItem { Value = "Damage", Text = "Damage/Waste" },
                new SelectListItem { Value = "Transfer", Text = "Transfer to Another Location" }
            };
            
            return View(model);
        }

        // POST: Admin/TeaInventory/RemoveStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.InventoryTransact)]
        public async Task<IActionResult> RemoveStock(StockTransactionViewModel model)
        {
            if (User.IsInRole(AppRoles.WarehouseStaff))
            {
                // Warehouse operators record quantities and operational reasons,
                // but do not enter or update financial values.
                model.UnitPrice = null;

                var permittedTypes = new[] { "Sale", "Damage", "Transfer" };
                if (!permittedTypes.Contains(model.TransactionType, StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(model.TransactionType),
                        "This stock-out operation is not permitted for Warehouse Staff.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _inventoryService.UpdateStockAsync(
                        model.ItemId,
                        model.Quantity,
                        model.TransactionType,
                        User.Identity?.Name ?? "System",
                        model.Notes,
                        null,
                        model.ReferenceNumber,
                        model.UnitPrice);
                        
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Stock removed successfully";
                        return RedirectToAction(nameof(Details), new { id = model.ItemId });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to remove stock. Check that you have sufficient quantity.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error removing stock: {ex.Message}");
                }
            }
            
            // If we got this far, something failed, redisplay form
            var item = await _context.TeaInventoryItems.FindAsync(model.ItemId);
            if (item != null)
            {
                model.CurrentStock = item.CurrentStock;
            }
            
            ViewBag.TransactionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "Sale", Text = "Sale" },
                new SelectListItem { Value = "Damage", Text = "Damage/Waste" },
                new SelectListItem { Value = "Transfer", Text = "Transfer to Another Location" }
            };
            
            return View(model);
        }
        
        // GET: Admin/TeaInventory/LowStock
        public async Task<IActionResult> LowStock()
        {
            try
            {
                var items = await _inventoryService.GetLowStockItemsAsync();
                
                // Ensure no null string values exist in the items
                foreach (var item in items)
                {
                    item.Name ??= string.Empty;
                    item.TeaType ??= string.Empty;
                    item.Grade ??= string.Empty;
                    item.Origin ??= string.Empty;
                    item.HarvestSeason ??= string.Empty;
                    item.BatchNumber ??= string.Empty;
                    item.Description ??= string.Empty;
                    item.Unit ??= string.Empty;
                    item.Status ??= string.Empty;
                    item.QRCodeData ??= string.Empty;
                    item.LastCorrectedBy ??= string.Empty;
                    item.CorrectionReason ??= string.Empty;
                }
                
                return View(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving low stock items: {ex}");
                TempData["ErrorMessage"] = $"Error retrieving low stock items: {ex.Message}";
                return View(new List<TeaInventoryItem>());
            }
        }

        private List<SelectListItem> GetStockInTransactionTypes()
        {
            var types = new List<SelectListItem>
            {
                new() { Value = "Delivery", Text = "Delivery" },
                new() { Value = "Production", Text = "Production" },
                new() { Value = "Return", Text = "Return from Customer" }
            };

            if (!User.IsInRole(AppRoles.WarehouseStaff))
            {
                types.Add(new SelectListItem
                {
                    Value = "Correction",
                    Text = "Correction/Adjustment"
                });
            }

            return types;
        }
    }
}
