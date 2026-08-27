using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AppPermissions.SupplierManage)]
    public class SupplierCategoriesController : AdminBaseController
    {
        private readonly TeaOnlineShopContext _context;

        public SupplierCategoriesController(TeaOnlineShopContext context)
        {
            _context = context;
        }

        // GET: Admin/SupplierCategories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.SupplierCategories.ToListAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error retrieving supplier categories: {ex.Message}";
                return View(new List<SupplierCategory>());
            }
        }

        // GET: Admin/SupplierCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.SupplierCategories
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Admin/SupplierCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/SupplierCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] SupplierCategory category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Category created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating category: {ex.Message}");
                }
            }
            
            return View(category);
        }

        // GET: Admin/SupplierCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.SupplierCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            return View(category);
        }

        // POST: Admin/SupplierCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] SupplierCategory category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Category updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
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
                    ModelState.AddModelError("", $"Error updating category: {ex.Message}");
                }
            }
            
            return View(category);
        }

        // GET: Admin/SupplierCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.SupplierCategories
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/SupplierCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.SupplierCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            try
            {
                _context.SupplierCategories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting category: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.SupplierCategories.Any(e => e.Id == id);
        }
    }
} 
