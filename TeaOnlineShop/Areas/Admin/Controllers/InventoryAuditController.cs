using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;

namespace TeaOnlineShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPermissions.AuditView)]
public sealed class InventoryAuditController : AdminBaseController
{
    private readonly TeaOnlineShopContext _context;

    public InventoryAuditController(TeaOnlineShopContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Ledger(DateTime? from, DateTime? to, string? itemCode, string? movementType)
    {
        var query = BuildLedgerQuery(from, to, itemCode, movementType);
        var entries = await query.Take(500).ToListAsync();
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.ItemCode = itemCode;
        ViewBag.MovementType = movementType;
        ViewBag.Truncated = entries.Count == 500;
        return View(entries);
    }

    [HttpGet]
    public async Task<IActionResult> ExportLedger(DateTime? from, DateTime? to, string? itemCode, string? movementType)
    {
        var entries = await BuildLedgerQuery(from, to, itemCode, movementType).Take(10000).ToListAsync();
        var csv = new StringBuilder("EntryNumber,OccurredUtc,ItemCode,ItemName,MovementType,QuantityChange,PreviousStock,NewStock,Warehouse,Bin,ReferenceType,ReferenceNumber,Reason,PerformedBy\r\n");
        foreach (var entry in entries)
        {
            csv.AppendLine(string.Join(',',
                Csv(entry.EntryNumber.ToString("D")),
                Csv(entry.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                Csv(entry.ItemCode), Csv(entry.ItemName), Csv(entry.MovementType),
                entry.QuantityChange.ToString(CultureInfo.InvariantCulture),
                entry.PreviousStock.ToString(CultureInfo.InvariantCulture),
                entry.NewStock.ToString(CultureInfo.InvariantCulture),
                Csv(entry.Warehouse.Code), Csv(entry.Bin?.Code ?? string.Empty),
                Csv(entry.ReferenceType), Csv(entry.ReferenceNumber), Csv(entry.Reason), Csv(entry.PerformedByName)));
        }
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"stock-ledger-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    public async Task<IActionResult> QrScans(DateTime? from, DateTime? to, string? entityType, bool? successful)
    {
        var query = _context.QRCodeScans.AsNoTracking();
        if (from.HasValue) query = query.Where(x => x.ScanDateTime >= from.Value.Date);
        if (to.HasValue) query = query.Where(x => x.ScanDateTime < to.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
        if (successful.HasValue) query = query.Where(x => x.WasSuccessful == successful.Value);
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.EntityType = entityType;
        ViewBag.Successful = successful;
        return View(await query.OrderByDescending(x => x.ScanDateTime).Take(500).ToListAsync());
    }

    public async Task<IActionResult> Integrity()
    {
        var model = new InventoryIntegrityViewModel
        {
            CheckedAtUtc = DateTime.UtcNow,
            LedgerEntries = await _context.StockLedgerEntries.CountAsync(),
            StockBalances = await _context.StockBalances.CountAsync(),
            ProductMappings = await _context.ProductInventoryMappings.CountAsync(),
            OrdersWithoutLines = await _context.Orders.CountAsync(x => !x.Lines.Any()),
            ReceivedDeliveriesWithoutLedger = await _context.Deliveries.CountAsync(d =>
                d.Status == "Received" && !_context.StockLedgerEntries.Any(l => l.ReferenceType == "Delivery" && l.ReferenceId == d.Id))
        };

        var teaItems = await _context.TeaInventoryItems.AsNoTracking().ToListAsync();
        var supplyItems = await _context.SupplyItems.AsNoTracking().ToListAsync();
        var balances = await _context.StockBalances.AsNoTracking().ToListAsync();
        foreach (var item in teaItems)
        {
            var total = balances.Where(x => x.InventoryItemId == item.Id).Sum(x => x.Quantity);
            if (total != item.CurrentStock)
                model.Issues.Add(Issue("Critical", "Tea inventory", item.ItemCode, $"Aggregate quantity {item.CurrentStock:0.####} differs from location balances {total:0.####}."));
        }
        foreach (var item in supplyItems)
        {
            var total = balances.Where(x => x.SupplyItemId == item.Id).Sum(x => x.Quantity);
            if (total != item.CurrentStock)
                model.Issues.Add(Issue("Critical", "Supply item", item.ItemCode, $"Aggregate quantity {item.CurrentStock:0.####} differs from location balances {total:0.####}."));
        }

        var products = await _context.Products.AsNoTracking().Include(x => x.InventoryMapping).ToListAsync();
        foreach (var product in products)
        {
            if (product.InventoryMapping is null)
            {
                model.Issues.Add(Issue("Critical", "Product", product.Id.ToString(), "Product has no inventory mapping and cannot be sold safely."));
                continue;
            }
            var total = balances.Where(x => x.InventoryItemId == product.InventoryMapping.InventoryItemId).Sum(x => x.Quantity);
            var expected = product.InventoryMapping.QuantityPerUnit <= 0 ? 0 : (int)Math.Floor(total / product.InventoryMapping.QuantityPerUnit);
            if (product.Quantity != expected)
                model.Issues.Add(Issue("Critical", "Product", product.Id.ToString(), $"Sellable quantity {product.Quantity} differs from mapped inventory quantity {expected}."));
        }

        if (model.OrdersWithoutLines > 0)
            model.Issues.Add(Issue("Warning", "Orders", "Legacy", $"{model.OrdersWithoutLines} order(s) have no immutable line snapshots."));
        if (model.ReceivedDeliveriesWithoutLedger > 0)
            model.Issues.Add(Issue("Critical", "Deliveries", "Unposted", $"{model.ReceivedDeliveriesWithoutLedger} received delivery record(s) have no stock-ledger posting."));
        return View(model);
    }

    private IQueryable<StockLedgerEntry> BuildLedgerQuery(DateTime? from, DateTime? to, string? itemCode, string? movementType)
    {
        var query = _context.StockLedgerEntries.AsNoTracking().Include(x => x.Warehouse).Include(x => x.Bin).AsQueryable();
        if (from.HasValue) query = query.Where(x => x.OccurredAtUtc >= from.Value.Date.ToUniversalTime());
        if (to.HasValue) query = query.Where(x => x.OccurredAtUtc < to.Value.Date.AddDays(1).ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(itemCode)) query = query.Where(x => x.ItemCode.Contains(itemCode.Trim()));
        if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(x => x.MovementType == movementType.Trim());
        return query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id);
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static InventoryIntegrityIssueViewModel Issue(string severity, string entity, string reference, string message) =>
        new() { Severity = severity, Entity = entity, Reference = reference, Message = message };
}
