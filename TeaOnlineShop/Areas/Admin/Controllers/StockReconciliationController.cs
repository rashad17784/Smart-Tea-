using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;

namespace TeaOnlineShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPermissions.InventoryView)]
public sealed class StockReconciliationController : AdminBaseController
{
    private readonly TeaOnlineShopContext _context;
    private readonly StockLedgerService _stockLedger;

    public StockReconciliationController(TeaOnlineShopContext context, StockLedgerService stockLedger)
    {
        _context = context;
        _stockLedger = stockLedger;
    }

    public async Task<IActionResult> Index()
    {
        var records = await _context.StockReconciliations
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Lines)
            .OrderByDescending(x => x.CountedAtUtc)
            .Take(100)
            .ToListAsync();
        return View(records);
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.InventoryTransact)]
    public async Task<IActionResult> Create()
    {
        var warehouse = await _context.Warehouses.AsNoTracking()
            .OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.IsActive);
        if (warehouse is null)
        {
            TempData["ErrorMessage"] = "No active warehouse is configured.";
            return RedirectToAction(nameof(Index));
        }
        return View(await BuildCountModelAsync(warehouse.Id));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissions.InventoryTransact)]
    public async Task<IActionResult> Create(StockReconciliationCreateViewModel model)
    {
        var current = await BuildCountModelAsync(model.WarehouseId);
        var submitted = model.Lines.ToDictionary(x => x.BalanceId);
        foreach (var line in current.Lines)
        {
            if (submitted.TryGetValue(line.BalanceId, out var posted))
            {
                line.CountedQuantity = posted.CountedQuantity;
                line.Reason = posted.Reason?.Trim();
            }
            if (!line.CountedQuantity.HasValue)
                ModelState.AddModelError($"Lines[{current.Lines.IndexOf(line)}].CountedQuantity", "A physical count is required for every item.");
            else if (line.CountedQuantity.Value != line.SystemQuantity && string.IsNullOrWhiteSpace(line.Reason))
                ModelState.AddModelError(string.Empty, $"A variance reason is required for {line.ItemCode}.");
        }
        current.Notes = model.Notes;
        if (!ModelState.IsValid)
            return View(current);

        var actorId = GetActorId();
        var record = new StockReconciliation
        {
            Id = Guid.NewGuid(),
            ReconciliationNumber = $"REC-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            WarehouseId = current.WarehouseId,
            Status = "PendingApproval",
            CountedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actorId,
            CreatedByName = User.Identity?.Name ?? "Unknown warehouse user",
            Notes = current.Notes
        };
        foreach (var line in current.Lines)
        {
            var counted = line.CountedQuantity!.Value;
            record.Lines.Add(new StockReconciliationLine
            {
                InventoryItemId = line.InventoryItemId,
                SupplyItemId = line.SupplyItemId,
                ItemCode = line.ItemCode,
                ItemName = line.ItemName,
                SystemQuantity = line.SystemQuantity,
                CountedQuantity = counted,
                Difference = counted - line.SystemQuantity,
                Reason = counted == line.SystemQuantity ? "Physical count matched the system balance." : line.Reason!
            });
        }

        _context.StockReconciliations.Add(record);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Physical count submitted. A different authorized administrator must approve any variances.";
        return RedirectToAction(nameof(Details), new { id = record.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var record = await _context.StockReconciliations.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Lines.OrderBy(l => l.ItemCode))
            .SingleOrDefaultAsync(x => x.Id == id);
        return record is null ? NotFound() : View(record);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
    public async Task<IActionResult> Approve(Guid id)
    {
        var actorId = GetActorId();
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var record = await _context.StockReconciliations
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (record is null)
            return NotFound();
        if (record.Status != "PendingApproval")
        {
            TempData["ErrorMessage"] = "Only a pending reconciliation can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (record.CreatedByUserId == actorId)
        {
            TempData["ErrorMessage"] = "Segregation of duties is enforced: the counter cannot approve the same reconciliation.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            foreach (var line in record.Lines.OrderBy(x => x.Id))
            {
                var balance = await _context.StockBalances.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.WarehouseId == record.WarehouseId &&
                        ((line.InventoryItemId.HasValue && x.InventoryItemId == line.InventoryItemId) ||
                         (line.SupplyItemId.HasValue && x.SupplyItemId == line.SupplyItemId)));
                var current = balance?.Quantity ?? 0m;
                if (current != line.SystemQuantity)
                    throw new InvalidOperationException($"{line.ItemCode} changed from {line.SystemQuantity:0.####} to {current:0.####} after the count. Create a new reconciliation.");

                var difference = line.CountedQuantity - current;
                if (difference == 0)
                    continue;
                var request = new StockMovementRequest
                {
                    MovementType = "ReconciliationAdjustment",
                    QuantityChange = difference,
                    ReferenceType = "StockReconciliation",
                    ReferenceNumber = record.ReconciliationNumber,
                    Reason = line.Reason,
                    PerformedByUserId = actorId,
                    PerformedByName = User.Identity?.Name ?? "Unknown administrator",
                    WarehouseId = record.WarehouseId,
                    BinId = balance?.BinId,
                    CorrelationId = record.Id
                };
                var ledger = line.InventoryItemId.HasValue
                    ? await _stockLedger.RecordTeaMovementAsync(line.InventoryItemId.Value, request)
                    : await _stockLedger.RecordSupplyMovementAsync(line.SupplyItemId!.Value, request);
                line.LedgerEntryId = ledger.Id;
            }

            record.Status = "Approved";
            record.ApprovedByUserId = actorId;
            record.ApprovedByName = User.Identity?.Name;
            record.ApprovedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Reconciliation approved and variances posted to the immutable ledger.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = $"Nothing was posted. Approval failed: {ex.Message}";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var record = await _context.StockReconciliations.SingleOrDefaultAsync(x => x.Id == id);
        if (record is null)
            return NotFound();
        if (record.Status != "PendingApproval")
            TempData["ErrorMessage"] = "Only a pending reconciliation can be rejected.";
        else if (string.IsNullOrWhiteSpace(reason))
            TempData["ErrorMessage"] = "A rejection reason is required.";
        else
        {
            record.Status = "Rejected";
            record.ApprovedByUserId = GetActorId();
            record.ApprovedByName = User.Identity?.Name;
            record.ApprovedAtUtc = DateTime.UtcNow;
            record.Notes = string.IsNullOrWhiteSpace(record.Notes)
                ? $"Rejected: {reason.Trim()}"
                : $"{record.Notes}{Environment.NewLine}Rejected: {reason.Trim()}";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Reconciliation rejected; no stock was changed.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<StockReconciliationCreateViewModel> BuildCountModelAsync(int warehouseId)
    {
        var warehouse = await _context.Warehouses.AsNoTracking().SingleAsync(x => x.Id == warehouseId && x.IsActive);
        var balances = await _context.StockBalances.AsNoTracking()
            .Include(x => x.Bin)
            .Where(x => x.WarehouseId == warehouseId)
            .OrderBy(x => x.Bin.Code)
            .ThenBy(x => x.Id)
            .ToListAsync();
        var inventoryIds = balances.Where(x => x.InventoryItemId.HasValue).Select(x => x.InventoryItemId!.Value).ToList();
        var supplyIds = balances.Where(x => x.SupplyItemId.HasValue).Select(x => x.SupplyItemId!.Value).ToList();
        var inventory = await _context.TeaInventoryItems.AsNoTracking().Where(x => inventoryIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var supplies = await _context.SupplyItems.AsNoTracking().Where(x => supplyIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

        return new StockReconciliationCreateViewModel
        {
            WarehouseId = warehouse.Id,
            WarehouseName = warehouse.Name,
            Lines = balances.Select(balance =>
            {
                var tea = balance.InventoryItemId.HasValue ? inventory[balance.InventoryItemId.Value] : null;
                var supply = balance.SupplyItemId.HasValue ? supplies[balance.SupplyItemId.Value] : null;
                return new StockReconciliationCountLineViewModel
                {
                    BalanceId = balance.Id,
                    InventoryItemId = balance.InventoryItemId,
                    SupplyItemId = balance.SupplyItemId,
                    ItemCode = tea?.ItemCode ?? supply!.ItemCode,
                    ItemName = tea?.Name ?? supply!.Name,
                    BinCode = balance.Bin.Code,
                    SystemQuantity = balance.Quantity
                };
            }).ToList()
        };
    }

    private int GetActorId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new InvalidOperationException("The authenticated user has no valid identifier.");
}
