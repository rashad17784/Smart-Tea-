using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;

namespace TeaOnlineShop.Controllers
{
    public class CartController : Controller
    {
        private readonly TeaOnlineShopContext _context;
        private readonly StockLedgerService _stockLedger;
        public CartController(TeaOnlineShopContext context, StockLedgerService stockLedger)
        {
            _context = context;
            _stockLedger = stockLedger;
        }
        public async Task<IActionResult> Index()
        {
            var result = GetProductsinCart();
            var cartItems = GetCartItems();
            var cartAdjusted = false;

            if (result != null)
            {
                foreach (var item in result.ToList())
                {
                    item.AvailableQuantity = await GetAvailableQuantityAsync(item.Id);
                    var cartItem = cartItems.First(x => x.ProductId == item.Id);

                    if (item.AvailableQuantity <= 0)
                    {
                        cartItems.Remove(cartItem);
                        result.Remove(item);
                        cartAdjusted = true;
                        continue;
                    }

                    if (item.Count > item.AvailableQuantity)
                    {
                        item.Count = item.AvailableQuantity;
                        item.RowSumPrice = item.Price.GetValueOrDefault() * item.Count;
                        cartItem.Count = item.AvailableQuantity;
                        cartAdjusted = true;
                    }
                }
            }

            if (cartAdjusted)
            {
                SaveCartItems(cartItems);
                ViewData["StockAdjustmentMessage"] =
                    "Your cart was adjusted to match the stock currently available.";
            }

            return View(result);
        }

        public IActionResult ClearCart()
        {
            Response.Cookies.Delete("Cart");
            return Redirect("/");
        }
        /// <summary>
        /// Add or update the shopping cart
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="count">
        ///  If quantity is zero, it means the intention is to remove the item. 
        /// This case is manually handled by us.
        /// </param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateCart([FromBody] CartViewModel request)
        {
            var product = _context.Products.FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            if (request.Count < 0)
            {
                return BadRequest(new
                {
                    message = "Quantity cannot be negative."
                });
            }

            // Retrieve the list of products in the cart using the dedicated function
            var cartItems = GetCartItems();

            var foundProductInCart = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);

            if (request.Count > 0)
            {
                var available = await GetAvailableQuantityAsync(request.ProductId);
                if (request.Count > available)
                {
                    return Conflict(new
                    {
                        message = available > 0
                            ? $"Only {available} unit{(available == 1 ? string.Empty : "s")} of {product.Title} are currently available."
                            : $"{product.Title} is currently out of stock.",
                        available,
                        currentQuantity = foundProductInCart?.Count ?? 0
                    });
                }
            }

            // If the product is found, it means it is in the cart, and the user intends to change the quantity
            if (foundProductInCart == null)
            {
                var newCartItem = new CartViewModel() { };
                newCartItem.ProductId = request.ProductId;
                newCartItem.Count = request.Count;

                cartItems.Add(newCartItem);
            }
            else
            {
                // If greater than zero, it means the user wants to update the quantity; otherwise, it will be removed from the cart.
                if (request.Count > 0)
                {
                    foundProductInCart.Count = request.Count;
                }
                else
                {
                    cartItems.Remove(foundProductInCart);
                }
            }

            SaveCartItems(cartItems);

            var result = cartItems.Sum(x => x.Count);

            return Ok(result);
        }

        private async Task<int> GetAvailableQuantityAsync(int productId)
        {
            var available = await _stockLedger.GetAvailableProductUnitsAsync(productId);
            if (available <= 0)
            {
                return 0;
            }

            return available >= int.MaxValue ? int.MaxValue : (int)available;
        }

        private void SaveCartItems(List<CartViewModel> cartItems)
        {
            var json = JsonConvert.SerializeObject(cartItems);
            var option = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            };
            Response.Cookies.Append("Cart", json, option);
        }

        public List<CartViewModel> GetCartItems()
        {
            List<CartViewModel> cartList = new List<CartViewModel>();

            var prevCartItemsString = Request.Cookies["Cart"];

            // If it's not null, it means the cart is not empty, so we need to convert it to a list of view models; 
            // otherwise, we return an empty cart list.
            if (!string.IsNullOrEmpty(prevCartItemsString))
            {
                cartList = JsonConvert.DeserializeObject<List<CartViewModel>>(prevCartItemsString);
            }

            return cartList;
        }

        public List<ProductCartViewModel> GetProductsinCart()
        {
            var cartItems = GetCartItems();

            if (!cartItems.Any())
            {
                return null ;
            }

            var cartItemProductIds = cartItems.Select(x => x.ProductId).ToList();
            // Load products into memory
            var products = _context.Products
                .Where(p => cartItemProductIds.Contains(p.Id))
                .ToList();

            // Create the ProductCartViewModel list

            List<ProductCartViewModel> result = new List<ProductCartViewModel>();
            foreach (var item in products)
            {
                var newItem = new ProductCartViewModel
                {
                    Id = item.Id,
                    ImageName = item.ImageName,
                    Price = item.Price - (item.Discount ?? 0),
                    Title = item.Title,
                    Count = cartItems.Single(x => x.ProductId == item.Id).Count,
                    RowSumPrice = (item.Price - (item.Discount ?? 0)) * cartItems.Single(x => x.ProductId == item.Id).Count,
                };

                result.Add(newItem);
            }

            return result ;
        }

        public IActionResult SmallCart()
        {
            var result = GetProductsinCart();
            return PartialView(result);
        }

        [Authorize]
        public IActionResult Checkout()
        {
            var order = new Models.Dbase.Order();

            var shipping = _context.Settings.First().Shipping;
            if (shipping != null)
            {
                order.Shipping = shipping;
            }

            ViewData["Products"] = GetProductsinCart();
            return View(order);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order, string PaymentMethod)
        {
            if (!string.Equals(PaymentMethod, "cash_on_delivery", StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(PaymentMethod), "The selected payment method is not available.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get products from cart
                    var cartProducts = GetProductsinCart();
                    if (cartProducts == null || !cartProducts.Any())
                    {
                        TempData["ErrorMessage"] = "Your cart is empty";
                        return RedirectToAction("Checkout");
                    }

                    var productIds = cartProducts.Select(x => x.Id).ToList();
                    var products = await _context.Products
                        .Include(x => x.InventoryMapping)
                        .Where(x => productIds.Contains(x.Id))
                        .ToDictionaryAsync(x => x.Id);

                    // Give the customer an early, friendly stock message. The ledger
                    // repeats this validation while holding a database lock.
                    foreach (var item in cartProducts.OrderBy(x => x.Id))
                    {
                        var available = await _stockLedger.GetAvailableProductUnitsAsync(item.Id);
                        if (!products.TryGetValue(item.Id, out var product) || available < item.Count)
                        {
                            TempData["ErrorMessage"] = $"Not enough stock for {item.Title}. Only {available:0} available.";
                            return RedirectToAction("Checkout");
                        }
                    }

                    // Process order
                    await using (var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
                    {
                        try
                        {
                            // Create new order
                            order.CreateDate = DateTime.Now;
                            order.Status = "Pending";
                            order.PaymentMethod = "CashOnDelivery";
                            order.PaymentStatus = "PendingCollection";
                            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
                            {
                                order.UserId = currentUserId;
                            }
                            var configuredShipping = await _context.Settings
                                .AsNoTracking()
                                .Select(x => x.Shipping)
                                .FirstOrDefaultAsync() ?? 0m;
                            order.Shipping = Math.Max(0m, configuredShipping);
                            order.SubTotal = cartProducts.Sum(x => x.RowSumPrice ?? 0);
                            order.Total = order.SubTotal + order.Shipping;
                            
                            order.TransId = "COD-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

                            _context.Orders.Add(order);
                            await _context.SaveChangesAsync();
                            _context.OrderStatusHistory.Add(new OrderStatusHistory
                            {
                                OrderId = order.Id,
                                FromStatus = string.Empty,
                                ToStatus = "Pending",
                                ChangedByUserId = order.UserId,
                                ChangedByName = User.Identity?.Name ?? order.Email,
                                Reason = "Customer order accepted and inventory committed.",
                                ChangedAtUtc = DateTime.UtcNow
                            });
                            _context.OrderPaymentEvents.Add(new OrderPaymentEvent
                            {
                                OrderId = order.Id,
                                FromStatus = string.Empty,
                                ToStatus = "PendingCollection",
                                Method = "CashOnDelivery",
                                Amount = order.Total ?? 0m,
                                Reference = order.TransId,
                                Reason = "Cash collection is due at delivery; no electronic payment was claimed.",
                                RecordedByUserId = order.UserId,
                                RecordedByName = User.Identity?.Name ?? order.Email,
                                RecordedAtUtc = DateTime.UtcNow
                            });

                            var correlationId = Guid.NewGuid();
                            foreach (var item in cartProducts.OrderBy(x => x.Id))
                            {
                                var product = products[item.Id];
                                var finalUnitPrice = (product.Price ?? 0m) - (product.Discount ?? 0m);
                                if (finalUnitPrice < 0)
                                {
                                    finalUnitPrice = 0;
                                }

                                var mapping = product.InventoryMapping
                                    ?? throw new InvalidOperationException($"Product '{product.Title}' has no inventory mapping.");

                                _context.OrderLines.Add(new OrderLine
                                {
                                    OrderId = order.Id,
                                    ProductId = product.Id,
                                    Sku = mapping.InventoryItemId > 0
                                        ? await _context.TeaInventoryItems
                                            .Where(x => x.Id == mapping.InventoryItemId)
                                            .Select(x => x.ItemCode)
                                            .SingleAsync()
                                        : $"PRODUCT-{product.Id}",
                                    ProductName = product.Title ?? $"Product {product.Id}",
                                    Quantity = item.Count,
                                    UnitPrice = finalUnitPrice,
                                    DiscountAmount = (product.Discount ?? 0m) * item.Count,
                                    TaxAmount = 0m,
                                    LineTotal = finalUnitPrice * item.Count,
                                    FulfilmentStatus = "Pending",
                                    CreatedAtUtc = DateTime.UtcNow
                                });

                                await _stockLedger.RecordProductSaleAsync(product.Id, item.Count, new StockMovementRequest
                                {
                                    MovementType = "CustomerOrder",
                                    ReferenceType = "Order",
                                    ReferenceId = order.Id,
                                    ReferenceNumber = order.TransId,
                                    Reason = $"Stock committed to customer order {order.TransId}.",
                                    PerformedByUserId = order.UserId,
                                    PerformedByName = User.Identity?.Name ?? order.Email,
                                    CorrelationId = correlationId
                                });
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            // Clear cart after successful order
                            Response.Cookies.Delete("Cart");

                            TempData["SuccessMessage"] = "Your order has been placed. Payment is due on delivery. Order ID: " + order.TransId;
                            
                            return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = "Error processing your order: " + ex.Message;
                            return RedirectToAction("Checkout");
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                    return RedirectToAction("Checkout");
                }
            }

            TempData["ErrorMessage"] = "Please fill in all required fields";
            ViewData["Products"] = GetProductsinCart();
            return View("Checkout", order);
        }

        [Authorize]
        public IActionResult OrderConfirmation(int orderId)
        {
            var currentUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : 0;
            var canViewAllOrders = User.HasClaim(AppPermissions.ClaimType, AppPermissions.ProductManage);
            var order = _context.Orders
                .Include(x => x.Lines)
                .FirstOrDefault(x => x.Id == orderId && (canViewAllOrders || x.UserId == currentUserId));
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

    }

}
