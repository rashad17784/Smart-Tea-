using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = TeaOnlineShop.Authorization.AppRoles.Administrator)]
    public class SupplierCategoriesController : Controller
    {
        private readonly TeaOnlineShopContext _context;

        public SupplierCategoriesController(TeaOnlineShopContext context)
        {
            _context = context;
        }

        // GET: SupplierCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.SupplierCategories.ToListAsync());
        }

        // GET: SupplierCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SupplierCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] SupplierCategory category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: SupplierCategories/Edit/5
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

        // POST: SupplierCategories/Edit/5
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
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: SupplierCategories/Delete/5
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

        // POST: SupplierCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Check if the category is in use
            bool isInUse = await _context.SupplierCategoryMappings.AnyAsync(m => m.CategoryId == id);
            
            if (isInUse)
            {
                TempData["ErrorMessage"] = "This category is in use by suppliers and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }
            
            var category = await _context.SupplierCategories.FindAsync(id);
            if (category != null)
            {
                _context.SupplierCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.SupplierCategories.Any(e => e.Id == id);
        }
    }
} 
