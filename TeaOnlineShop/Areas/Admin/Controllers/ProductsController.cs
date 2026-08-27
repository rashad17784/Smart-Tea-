using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Services;
using System.Security.Claims;

namespace TeaOnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = AppPermissions.ProductManage)]
    public class ProductsController : AdminBaseController
    {
        private readonly TeaOnlineShopContext _context;
        private readonly StockLedgerService _stockLedger;

        public ProductsController(TeaOnlineShopContext context, StockLedgerService stockLedger)
        {
            _context = context;
            _stockLedger = stockLedger;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }
        //======delete product in gallery============
        public IActionResult DeleteGallery(int id)
        {
            var gallery = _context.ProductGalleries.FirstOrDefault(x => x.Id == id);
            if (gallery == null)
            {
                return NotFound();
            }
            string d = Directory.GetCurrentDirectory();
            string fn = d + "\\wwwroot\\images\\banners\\" + gallery.ImageName;

            if (System.IO.File.Exists(fn))
            {
                System.IO.File.Delete(fn);
            }
            _context.Remove(gallery);
            _context.SaveChanges();

            return Redirect("edit/" + gallery.ProductId);
        }
        //==========================================================
        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,FullDescription,Price,Discount,ImageName,Quantity,Tags,VideoUrl")] Product product ,IFormFile? MainImage, IFormFile[]? GalleryImages)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var openingQuantity = product.Quantity;
                product.Quantity = 0;
                //------saving main image--------
                if (MainImage != null)
                {
                    product.ImageName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(MainImage.FileName);
                    string fn;
                    fn = Directory.GetCurrentDirectory();
                    string ImagePath = fn + "\\wwwroot\\images\\banners\\" + product.ImageName ;

                    using (var stream = new FileStream(ImagePath, FileMode.Create))
                    {
                        MainImage.CopyTo(stream);
                    }
                }

                //-------------------------------
                _context.Add(product);
                await _context.SaveChangesAsync();

                var inventoryItem = new TeaInventoryItem
                {
                    ItemCode = $"PROD-{product.Id}",
                    Name = product.Title ?? $"Product {product.Id}",
                    TeaType = "Finished Product",
                    Grade = "Retail",
                    Description = product.Description ?? string.Empty,
                    CurrentStock = 0,
                    Unit = "unit",
                    MinimumStock = 0,
                    RetailPrice = product.Price,
                    Status = "Active",
                    QRCodeData = $"PRODUCT:{product.Id}",
                    CreatedDate = DateTime.Now
                };
                _context.TeaInventoryItems.Add(inventoryItem);
                await _context.SaveChangesAsync();
                _context.ProductInventoryMappings.Add(new ProductInventoryMapping
                {
                    ProductId = product.Id,
                    InventoryItemId = inventoryItem.Id,
                    QuantityPerUnit = 1m,
                    IsActive = true
                });
                await _context.SaveChangesAsync();

                if (openingQuantity > 0)
                {
                    await _stockLedger.RecordTeaMovementAsync(inventoryItem.Id, new StockMovementRequest
                    {
                        MovementType = "OpeningBalance",
                        QuantityChange = openingQuantity,
                        ReferenceType = "ProductOnboarding",
                        ReferenceId = product.Id,
                        ReferenceNumber = inventoryItem.ItemCode,
                        Reason = "Opening balance recorded when the product was created.",
                        PerformedByUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
                            ? actorId
                            : null,
                        PerformedByName = User.Identity?.Name ?? "Unknown staff user"
                    });
                }
                //========saving the gallery images part==========
                if (GalleryImages != null)
                {
                    foreach (var item in GalleryImages)
                    {
                        var newgallery = new ProductGallery();
                        newgallery.ProductId = product.Id;
                        //----------------------------
                        newgallery.ImageName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(item.FileName);
                        string fn;
                        fn = Directory.GetCurrentDirectory();
                        string ImagePath = fn + "\\wwwroot\\images\\banners\\" + newgallery.ImageName;

                        using (var stream = new FileStream(ImagePath, FileMode.Create))
                        {
                            item.CopyTo(stream);
                        }
                        //----------------------------
                        _context.ProductGalleries.Add(newgallery);
                    }
                }
                //-----------------------------------------------
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                //================================================
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            //-----------------
            ViewData["gallery"]=_context.ProductGalleries.Where(x=> x.ProductId==product.Id).ToList();
            //-----------------
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,FullDescription,Price,Discount,ImageName,Quantity,Tags,VideoUrl")] Product product, IFormFile? MainImage, IFormFile[]? GalleryImages)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                   var existing = await _context.Products
                       .Include(x => x.InventoryMapping)
                       .ThenInclude(x => x!.InventoryItem)
                       .SingleOrDefaultAsync(x => x.Id == id);
                   if (existing == null)
                   {
                       return NotFound();
                   }

                   //----saving the  image-------
                    //========================================================
 if (MainImage != null)
 {
     string d = Directory.GetCurrentDirectory();
     string fn = d + "\\wwwroot\\images\\banners\\" + existing.ImageName;
     //------------------------------------------------
     if (System.IO.File.Exists(fn))
     {
         System.IO.File.Delete(fn);
     }
     //------------------------------------------------
     using (var stream = new FileStream(fn, FileMode.Create))
     {
         MainImage.CopyTo(stream);
     }
     //------------------------------------------------
 }
 //========================================================
 if (GalleryImages != null)
 {
     foreach (var item in GalleryImages)
     {

         var imageName = Guid.NewGuid() + Path.GetExtension(item.FileName);
         //------------------------------------------------
         string d = Directory.GetCurrentDirectory();
         string fn = d + "\\wwwroot\\images\\banners\\" + imageName;
         //------------------------------------------------
         using (var stream = new FileStream(fn, FileMode.Create))
         {
             item.CopyTo(stream);
         }
         //------------------------------------------------
         var galleryItem = new ProductGallery();
         galleryItem.ImageName = imageName;
         galleryItem.ProductId = product.Id;
         //------------------------------------------------
         _context.ProductGalleries.Add(galleryItem);
     }
 }
 //========================================================
                   //----------------------------
                    
                    existing.Title = product.Title;
                    existing.Description = product.Description;
                    existing.FullDescription = product.FullDescription;
                    existing.Price = product.Price;
                    existing.Discount = product.Discount;
                    existing.Tags = product.Tags;
                    existing.VideoUrl = product.VideoUrl;

                    if (existing.InventoryMapping?.InventoryItem is not null)
                    {
                        existing.InventoryMapping.InventoryItem.Name = product.Title ?? $"Product {product.Id}";
                        existing.InventoryMapping.InventoryItem.Description = product.Description ?? string.Empty;
                        existing.InventoryMapping.InventoryItem.RetailPrice = product.Price;
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                //---------deleting the images----------------
                string d = Directory.GetCurrentDirectory();
                string fn = d + "\\wwwroot\\images\\banners\\";
                //----------
                string mainImagePath = fn + product.ImageName;
                //--------------------------
                if (System.IO.File.Exists(mainImagePath))
                {
                    System.IO.File.Delete(mainImagePath);
                }
                //--------------------------
                var galleries = _context.ProductGalleries.Where(x => x.ProductId == id).ToList();
                if (galleries != null)
                {
                    //--------------------------
                    foreach (var item in galleries)
                    {
                        string galleryImagePath = fn + item.ImageName;
                        //--------------------------
                        if (System.IO.File.Exists(galleryImagePath))
                        {
                            System.IO.File.Delete(galleryImagePath);
                        }
                        //--------------------------
                    }
                    //--------------------------
                    _context.ProductGalleries.RemoveRange(galleries);
                }
                //--------------------------------------------
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
