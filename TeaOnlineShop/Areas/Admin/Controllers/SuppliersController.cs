using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using TeaOnlineShop.Authorization;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AppPermissions.SupplierView)]
    public class SuppliersController : AdminBaseController
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

        // GET: Admin/Suppliers
        public async Task<IActionResult> Index()
        {
            try
            {
                var suppliers = await _context.Suppliers.ToListAsync();
                TempData["SupplierCount"] = suppliers.Count;
                
                // Return the view within the Admin area
                return View(suppliers);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving suppliers: {ex.Message}";
                return View(new List<Supplier>());
            }
        }

        // GET: Admin/Suppliers/Details/5
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

            return View(supplier);
        }

        // GET: Admin/Suppliers/Create
        [Authorize(Policy = AppPermissions.SupplierManage)]
        public async Task<IActionResult> Create()
        {
            // Generate a unique QR code for new supplier
            var viewModel = new SupplierViewModel
            {
                RegistrationDate = DateTime.Now,
                Status = "Active",
                QRCodeData = _qrCodeService.GenerateUniqueQRCodeData("SUP" + DateTime.Now.ToString("yyMMdd"))
            };
            
            // Populate available categories
            viewModel.AvailableCategories = await _context.SupplierCategories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
            
            return View(viewModel);
        }

        // POST: Admin/Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.SupplierManage)]
        public async Task<IActionResult> Create(SupplierViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to entity
                    var supplier = new Supplier
                    {
                        SupplierCode = viewModel.SupplierCode,
                        Name = viewModel.Name,
                        ContactPerson = viewModel.ContactPerson,
                        Phone = viewModel.Phone,
                        Email = viewModel.Email,
                        Address = viewModel.Address,
                        RegistrationDate = viewModel.RegistrationDate,
                        QRCodeData = viewModel.QRCodeData,
                        Status = viewModel.Status,
                        Notes = viewModel.Notes
                    };
                    
                    _context.Add(supplier);
                    await _context.SaveChangesAsync();
                    
                    // Save category mappings
                    if (viewModel.SelectedCategoryIds != null && viewModel.SelectedCategoryIds.Any())
                    {
                        foreach (var categoryId in viewModel.SelectedCategoryIds)
                        {
                            _context.Add(new SupplierCategoryMapping
                            {
                                SupplierId = supplier.Id,
                                CategoryId = categoryId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["SuccessMessage"] = "Supplier created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating supplier: {ex.Message}");
                }
            }
            
            // Repopulate categories dropdown if we got this far (validation failed)
            viewModel.AvailableCategories = await _context.SupplierCategories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
            
            return View(viewModel);
        }

        // GET: Admin/Suppliers/Edit/5
        [Authorize(Policy = AppPermissions.SupplierManage)]
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
            
            // Map entity to view model
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
                Notes = supplier.Notes
            };
            
            // Get selected category ids
            viewModel.SelectedCategoryIds = await _context.SupplierCategoryMappings
                .Where(m => m.SupplierId == id)
                .Select(m => m.CategoryId)
                .ToListAsync();
                
            // Populate available categories
            viewModel.AvailableCategories = await _context.SupplierCategories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = viewModel.SelectedCategoryIds.Contains(c.Id)
                })
                .ToListAsync();
            
            return View(viewModel);
        }

        // POST: Admin/Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.SupplierManage)]
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
                    // Map view model to entity
                    var supplier = await _context.Suppliers.FindAsync(id);
                    if (supplier == null)
                    {
                        return NotFound();
                    }
                    
                    supplier.SupplierCode = viewModel.SupplierCode;
                    supplier.Name = viewModel.Name;
                    supplier.ContactPerson = viewModel.ContactPerson;
                    supplier.Phone = viewModel.Phone;
                    supplier.Email = viewModel.Email;
                    supplier.Address = viewModel.Address;
                    supplier.Status = viewModel.Status;
                    supplier.Notes = viewModel.Notes;
                    
                    _context.Update(supplier);
                    
                    // Update category mappings
                    // First remove existing mappings
                    var existingMappings = await _context.SupplierCategoryMappings
                        .Where(m => m.SupplierId == id)
                        .ToListAsync();
                        
                    _context.SupplierCategoryMappings.RemoveRange(existingMappings);
                    
                    // Then add new ones
                    if (viewModel.SelectedCategoryIds != null && viewModel.SelectedCategoryIds.Any())
                    {
                        foreach (var categoryId in viewModel.SelectedCategoryIds)
                        {
                            _context.Add(new SupplierCategoryMapping
                            {
                                SupplierId = supplier.Id,
                                CategoryId = categoryId
                            });
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Supplier updated successfully";
                    return RedirectToAction(nameof(Index));
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
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating supplier: {ex.Message}");
                }
            }
            
            // Repopulate categories dropdown if we got this far
            viewModel.AvailableCategories = await _context.SupplierCategories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = viewModel.SelectedCategoryIds.Contains(c.Id)
                })
                .ToListAsync();
            
            return View(viewModel);
        }

        // GET: Admin/Suppliers/Delete/5
        [Authorize(Policy = AppPermissions.SupplierManage)]
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

        // POST: Admin/Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = AppPermissions.SupplierManage)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            
            try
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Supplier deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting supplier: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Suppliers/Scan
        public IActionResult Scan()
        {
            return View();
        }

        // GET: Admin/Suppliers/FindByCode
        public async Task<IActionResult> FindByCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Supplier code is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.SupplierCode == code || s.QRCodeData == code);
                
                if (supplier == null)
                {
                    await _qrAuditService.RecordAsync(code, "Supplier", null, false,
                        "NotFound", "Lookup", "No supplier matched the scanned code.");
                    TempData["ErrorMessage"] = $"No supplier found with code: {code}";
                    return RedirectToAction(nameof(Index));
                }

                await _qrAuditService.RecordAsync(code, "Supplier", supplier.Id, true,
                    "Found", "Lookup");
                return RedirectToAction(nameof(Details), new { id = supplier.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error finding supplier: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Suppliers/GetSupplierByCode
        [HttpGet]
        public async Task<IActionResult> GetSupplierByCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Json(null);
            }

            try
            {
                var supplier = await _context.Suppliers
                    .Where(s => s.SupplierCode == code || s.QRCodeData == code)
                    .Select(s => new 
                    {
                        id = s.Id,
                        name = s.Name,
                        supplierCode = s.SupplierCode,
                        contactPerson = s.ContactPerson,
                        phone = s.Phone,
                        email = s.Email,
                        status = s.Status
                    })
                    .FirstOrDefaultAsync();

                await _qrAuditService.RecordAsync(code, "Supplier", supplier?.id,
                    supplier is not null, supplier is null ? "NotFound" : "Found", "AjaxLookup");
                return Json(supplier);
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error getting supplier by code: {ex}");
                return Json(null);
            }
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
} 
