using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;

namespace TeaOnlineShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPermissions.OrdersView)]
public sealed class OrdersController : AdminBaseController
{
    private readonly TeaOnlineShopContext _context;

    public OrdersController(TeaOnlineShopContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.Orders.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        ViewBag.Status = status;
        return View(await query.OrderByDescending(x => x.CreateDate).Take(250).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders.AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.StatusHistory.OrderByDescending(h => h.ChangedAtUtc))
            .Include(x => x.PaymentEvents.OrderByDescending(h => h.RecordedAtUtc))
            .SingleOrDefaultAsync(x => x.Id == id);
        if (order is null)
            return NotFound();
        ViewBag.ShipModel = new ShipOrderViewModel { OrderId = id };
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissions.OrdersShip)]
    public async Task<IActionResult> Ship(ShipOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = model.OrderId });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var order = await _context.Orders.FromSqlInterpolated($@"
SELECT * FROM [dbo].[Order] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.OrderId}")
                .SingleOrDefaultAsync();
            if (order is null)
                return NotFound();
            if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Only Pending orders can be shipped. Current status: {order.Status}.");
            var lines = await _context.OrderLines.Where(x => x.OrderId == order.Id).ToListAsync();
            if (lines.Count == 0)
                throw new InvalidOperationException("This order has no line snapshots and cannot be shipped safely.");

            var actorId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
            var actorName = User.Identity?.Name ?? "Unknown warehouse user";
            var previous = order.Status ?? string.Empty;
            order.Status = "Shipped";
            order.Carrier = model.Carrier.Trim();
            order.TrackingNumber = model.TrackingNumber.Trim();
            order.ShippedAtUtc = DateTime.UtcNow;
            order.ShippedByUserId = actorId;
            order.ShippedByName = actorName;
            foreach (var line in lines)
                line.FulfilmentStatus = "Shipped";

            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = previous,
                ToStatus = "Shipped",
                ChangedByUserId = actorId,
                ChangedByName = actorName,
                Reason = $"{model.Reason.Trim()} Carrier: {order.Carrier}; tracking: {order.TrackingNumber}.",
                ChangedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Order {order.TransId} marked as shipped.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id = model.OrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissions.OrdersRecordPayment)]
    public async Task<IActionResult> RecordPayment(RecordOrderPaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values
                .SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = model.OrderId });
        }

        await using var transaction = await _context.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var order = await _context.Orders.FromSqlInterpolated($@"
SELECT * FROM [dbo].[Order] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {model.OrderId}")
                .SingleOrDefaultAsync();
            if (order is null)
                return NotFound();
            if (!string.Equals(order.PaymentMethod, "CashOnDelivery", StringComparison.Ordinal))
                throw new InvalidOperationException("Only cash-on-delivery collection can be recorded here.");
            if (!string.Equals(order.Status, "Shipped", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Payment collection can only be recorded after dispatch.");
            if (string.Equals(order.PaymentStatus, "Collected", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Payment has already been recorded for this order.");

            var actorId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
            var actorName = User.Identity?.Name ?? "Unknown payment operator";
            var previous = order.PaymentStatus;
            order.PaymentStatus = "Collected";
            _context.OrderPaymentEvents.Add(new OrderPaymentEvent
            {
                OrderId = order.Id,
                FromStatus = previous,
                ToStatus = "Collected",
                Method = order.PaymentMethod,
                Amount = order.Total ?? 0m,
                Reference = model.Reference.Trim(),
                Reason = model.Reason.Trim(),
                RecordedByUserId = actorId,
                RecordedByName = actorName,
                RecordedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = $"Payment collection recorded for order {order.TransId}.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = model.OrderId });
    }
}
