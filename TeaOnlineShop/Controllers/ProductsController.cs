using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TeaOnlineShop.Models.Dbase.TeaOnlineShopContext _context;
        public ProductsController(TeaOnlineShopContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            List<Product>  products=_context.Products.OrderByDescending(x=> x.Id).ToList();
            return View(products);
        }

        public IActionResult SearchProducts(string SearchText)
        {
           var products = _context.Products
                .Where(x =>
                EF.Functions.Like(x.Title,"%"+ SearchText+"%") ||
                EF.Functions.Like(x.Tags,"%"+ SearchText+"%"))
                .OrderBy(x => x.Title )
                .ToList();
            return View("Index",products);
        }
        public IActionResult ProductDetails(int id)
        {
            Product? product = _context.Products.FirstOrDefault(x => x.Id == id);
            //----------------
            if (product == null)
            { 
                return NotFound();
            }
            
            // Check if product quantity is valid
            if (product.Quantity < 0)
            {
                product.Quantity = 0;
                _context.Products.Update(product);
                _context.SaveChanges();
            }
            
            //========================
            ViewData["gallery"]=_context.ProductGalleries.Where(x=>x.ProductId==id).ToList();
            //-----------------
            ViewData["ShopProducts"]=_context.Products.Where(x=>x.Id!=id && x.Quantity > 0).ToList().
                Take(8).OrderByDescending(x=>x.Id).ToList();
            //-----------------
            ViewData["Comments"] = _context.CommentSections.Where(x => x.ProductId == id).
                                                    OrderByDescending(x => x.CreateDate).ToList();
            //-----------------
            return View(product);
          
        }
        [HttpPost]
        public IActionResult SubmitComment(string name, string email, string comment, int productId)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(comment) && productId != 0)
            {
                //----------------email validation part------------------------------
                Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");//email regular expression
                Match match = regex.Match(email);
                if (!match.Success)
                {
                    TempData["ErrorMessage"] = "Email is not valid";
                    return Redirect("/Products/ProductDetails/" + productId);
                }
                //-------------------------------------------------------------------
                CommentSection newComment = new CommentSection();
                newComment.Name = name;
                newComment.Email = email;
                newComment.CommmentText = comment;
                newComment.ProductId = productId;
                newComment.CreateDate = DateTime.Now;

                _context.CommentSections.Add(newComment);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your comment has been successfully submited!";
                return Redirect("/Products/ProductDetails/" + productId);
            }
            else
            {
                TempData["ErrorMessage"] = "Please complete your information";
                return Redirect("/Products/ProductDetails/" + productId);
            }

        }

    }
}
