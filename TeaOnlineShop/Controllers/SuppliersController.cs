using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;

namespace TeaOnlineShop.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = TeaOnlineShop.Authorization.AppRoles.Administrator)]
    public class SuppliersController : Controller
    {
        private readonly TeaOnlineShopContext _context;
        private readonly QRCodeService _qrCodeService;
        private readonly QrAuditService _qrAuditService;

        public SuppliersController(
            TeaOnlineShopContext context,
            QRCodeService qrCodeService,
            QrAuditService qrAuditService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _qrAuditService = qrAuditService;
        }

        // GET: Suppliers
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
                
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Supplier'";
                var tableExists = (int)command.ExecuteScalar() > 0;
                
                if (!tableExists)
                {
                    TempData["ErrorMessage"] = "Supplier schema is unavailable. Apply the approved database migrations before using this module.";
                    var suppliers = new List<Supplier>();
                    TempData["SupplierCount"] = 0;
                    return View(suppliers);
                }
                
                var suppliersList = await _context.Suppliers.ToListAsync();
                TempData["SupplierCount"] = suppliersList.Count;
                return View(suppliersList);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error fetching suppliers: {ex.Message}";
                return View(new List<Supplier>());
            }
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (supplier == null)
            {
                return NotFound();
            }

            // Get categories for this supplier
            var categoriesMappings = await _context.SupplierCategoryMappings
                .Include(m => m.Category)
                .Where(m => m.SupplierId == id)
                .ToListAsync();
                
            var categories = categoriesMappings.Select(m => m.Category).ToList();
            
            // Generate QR code for display
            byte[] qrCodeBytes = _qrCodeService.GenerateQRCode(supplier.QRCodeData);
            ViewBag.QRCodeImageUrl = _qrCodeService.GetDataUrl(qrCodeBytes);
            ViewBag.Categories = categories;

            return View(supplier);
        }

        // GET: Suppliers/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new SupplierViewModel
                {
                    // Generate a unique supplier code (e.g., SUP-001, SUP-002, etc.)
                    SupplierCode = await GenerateSupplierCode(),
                    AvailableCategories = await GetCategoriesSelectList(),
                    RegistrationDate = DateTime.Now
                };
                
                // Check if QR code data was passed (from scan)
                if (!string.IsNullOrEmpty(Request.Query["qrCode"]))
                {
                    viewModel.QRCodeData = Request.Query["qrCode"];
                }
                
                TempData["Message"] = "Debug: Create action called successfully. Available categories: " + viewModel.AvailableCategories.Count;
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error preparing supplier form: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierViewModel viewModel)
        {
            try
            {
                // Remove any model errors related to QRCodeData as we'll generate it
                if (ModelState.ContainsKey("QRCodeData"))
                {
                    ModelState.Remove("QRCodeData");
                }
                
                if (ModelState.IsValid)
                {
                    // Generate QR Code data
                    string qrCodeData = _qrCodeService.GenerateUniqueQRCodeData(viewModel.SupplierCode);
                    
                    // Create new supplier
                    var supplier = new Supplier
                    {
                        SupplierCode = viewModel.SupplierCode,
                        Name = viewModel.Name,
                        ContactPerson = viewModel.ContactPerson,
                        Phone = viewModel.Phone,
                        Email = viewModel.Email,
                        Address = viewModel.Address,
                        RegistrationDate = viewModel.RegistrationDate,
                        QRCodeData = qrCodeData,
                        Status = viewModel.Status,
                        Notes = viewModel.Notes
                    };
                    
                    _context.Add(supplier);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Supplier {supplier.Name} created with ID: {supplier.Id}";
                    
                    // Save category mappings
                    if (viewModel.SelectedCategoryIds != null && viewModel.SelectedCategoryIds.Count > 0)
                    {
                        foreach (var categoryId in viewModel.SelectedCategoryIds)
                        {
                            var mapping = new SupplierCategoryMapping
                            {
                                SupplierId = supplier.Id,
                                CategoryId = categoryId
                            };
                            _context.SupplierCategoryMappings.Add(mapping);
                        }
                        await _context.SaveChangesAsync();
                    }
                    
                    return RedirectToAction(nameof(Details), new { id = supplier.Id });
                }
                else
                {
                    // Debug model state errors
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    TempData["ErrorMessage"] = $"Validation errors: {errors}";
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving supplier: {ex.Message}");
                TempData["ErrorMessage"] = $"Error saving supplier: {ex.Message}";
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.AvailableCategories = await GetCategoriesSelectList();
            return View(viewModel);
        }

        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            
            // Get selected categories
            var selectedCategoryIds = await _context.SupplierCategoryMappings
                .Where(m => m.SupplierId == id)
                .Select(m => m.CategoryId)
                .ToListAsync();
                
            var viewModel = new SupplierViewModel
            {
                Id = supplier.Id,
                SupplierCode = supplier.SupplierCode,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                RegistrationDate = supplier.RegistrationDate,
                QRCodeData = supplier.QRCodeData,
                Status = supplier.Status,
                Notes = supplier.Notes,
                SelectedCategoryIds = selectedCategoryIds,
                AvailableCategories = await GetCategoriesSelectList()
            };
            
            // Generate QR code for display
            byte[] qrCodeBytes = _qrCodeService.GenerateQRCode(supplier.QRCodeData);
            viewModel.QRCodeImageUrl = _qrCodeService.GetDataUrl(qrCodeBytes);
            
            return View(viewModel);
        }

        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var supplier = await _context.Suppliers.FindAsync(id);
                    if (supplier == null)
                    {
                        return NotFound();
                    }
                    
                    // Update supplier
                    supplier.SupplierCode = viewModel.SupplierCode;
                    supplier.Name = viewModel.Name;
                    supplier.ContactPerson = viewModel.ContactPerson;
                    supplier.Phone = viewModel.Phone;
                    supplier.Email = viewModel.Email;
                    supplier.Address = viewModel.Address;
                    supplier.RegistrationDate = viewModel.RegistrationDate;
                    supplier.Status = viewModel.Status;
                    supplier.Notes = viewModel.Notes;
                    
                    _context.Update(supplier);
                    
                    // Update category mappings
                    var existingMappings = await _context.SupplierCategoryMappings
                        .Where(m => m.SupplierId == id)
                        .ToListAsync();
                        
                    _context.SupplierCategoryMappings.RemoveRange(existingMappings);
                    
                    if (viewModel.SelectedCategoryIds != null && viewModel.SelectedCategoryIds.Count > 0)
                    {
                        foreach (var categoryId in viewModel.SelectedCategoryIds)
                        {
                            var mapping = new SupplierCategoryMapping
                            {
                                SupplierId = supplier.Id,
                                CategoryId = categoryId
                            };
                            _context.SupplierCategoryMappings.Add(mapping);
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.AvailableCategories = await GetCategoriesSelectList();
            return View(viewModel);
        }

        // GET: Suppliers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                // Find and delete related category mappings
                var mappings = await _context.SupplierCategoryMappings
                    .Where(m => m.SupplierId == id)
                    .ToListAsync();
                    
                _context.SupplierCategoryMappings.RemoveRange(mappings);
                
                // Check for any deliveries from this supplier
                var deliveries = await _context.Deliveries
                    .Where(d => d.SupplierId == id)
                    .ToListAsync();
                    
                if (deliveries.Any())
                {
                    // Don't delete the supplier, just mark as inactive
                    supplier.Status = "Inactive";
                    _context.Update(supplier);
                    
                    TempData["Message"] = "Supplier has existing deliveries and cannot be deleted. It has been marked as inactive instead.";
                }
                else
                {
                    _context.Suppliers.Remove(supplier);
                }
                
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Suppliers/Scan
        public IActionResult Scan()
        {
            try
            {
                // Add debugging information
                TempData["Message"] = "Debug: Scan action called successfully. Please click the Start Scanner button below.";
                return View(new QRScanViewModel());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading scan page: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Suppliers/ProcessScan
        [HttpPost]
        public async Task<IActionResult> ProcessScan(QRScanViewModel model)
        {
            if (string.IsNullOrEmpty(model.QRCodeData))
            {
                return Json(new { success = false, message = "QR code data is required" });
            }
            
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.QRCodeData == model.QRCodeData);
                
            if (supplier != null)
            {
                await _qrAuditService.RecordAsync(model.QRCodeData, "Supplier", supplier.Id, true,
                    "Found", "Lookup", $"Supplier found: {supplier.Name}");
                
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
                await _qrAuditService.RecordAsync(model.QRCodeData, "Supplier", null, false,
                    "NotFound", "Lookup", "No supplier matched the scanned code.");
                
                return Json(new 
                { 
                    success = false, 
                    message = "No supplier found with this QR code" 
                });
            }
        }

        // Helper methods
        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
        
        private async Task<string> GenerateSupplierCode()
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
                
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Supplier'";
                var tableExists = (int)command.ExecuteScalar() > 0;
                
                if (!tableExists)
                {
                    throw new InvalidOperationException("Supplier schema is unavailable. Apply database migrations.");
                }
                
                int supplierCount = await _context.Suppliers.CountAsync();
                return $"SUP-{(supplierCount + 1):000}";
            }
            catch (Exception)
            {
                // In case of error, return a safe default
                return $"SUP-{DateTime.Now.ToString("yyMMdd")}-001";
            }
        }
        
        private async Task<List<SelectListItem>> GetCategoriesSelectList()
        {
            var categories = await _context.SupplierCategories.ToListAsync();
            return categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
        }
    }
} 
