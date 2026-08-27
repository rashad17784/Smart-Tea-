using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AppPermissions.ContentManage)]
    public class CommentSectionsController : AdminBaseController
    {
        private readonly TeaOnlineShopContext _context;

        public CommentSectionsController(TeaOnlineShopContext context)
        {
            _context = context;
        }

        // GET: Admin/CommentSections
        public async Task<IActionResult> Index()
        {
            return View(await _context.CommentSections.ToListAsync());
        }

        // GET: Admin/CommentSections/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var commentSection = await _context.CommentSections
                .FirstOrDefaultAsync(m => m.Id == id);
            if (commentSection == null)
            {
                return NotFound();
            }

            return View(commentSection);
        }

        // GET: Admin/CommentSections/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/CommentSections/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,CommmentText,ProductId,CreateDate")] CommentSection commentSection)
        {
            if (ModelState.IsValid)
            {
                _context.Add(commentSection);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(commentSection);
        }

        // GET: Admin/CommentSections/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var commentSection = await _context.CommentSections.FindAsync(id);
            if (commentSection == null)
            {
                return NotFound();
            }
            return View(commentSection);
        }

        // POST: Admin/CommentSections/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,CommmentText,ProductId,CreateDate")] CommentSection commentSection)
        {
            if (id != commentSection.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(commentSection);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentSectionExists(commentSection.Id))
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
            return View(commentSection);
        }

        // GET: Admin/CommentSections/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var commentSection = await _context.CommentSections
                .FirstOrDefaultAsync(m => m.Id == id);
            if (commentSection == null)
            {
                return NotFound();
            }

            return View(commentSection);
        }

        // POST: Admin/CommentSections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var commentSection = await _context.CommentSections.FindAsync(id);
            if (commentSection != null)
            {
                _context.CommentSections.Remove(commentSection);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommentSectionExists(int id)
        {
            return _context.CommentSections.Any(e => e.Id == id);
        }
    }
}
