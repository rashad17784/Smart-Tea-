using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using TeaOnlineShop.Services;

namespace TeaOnlineShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPermissions.InventoryMasterDataManage)]
public sealed class InventoryOnboardingController : AdminBaseController
{
    private const long MaximumFileSize = 2 * 1024 * 1024;
    private static readonly string[] RequiredHeaders =
    {
        "ItemType", "ItemCode", "Quantity", "UnitCost", "WarehouseCode", "BinCode", "Reason"
    };

    private readonly TeaOnlineShopContext _context;
    private readonly StockLedgerService _stockLedger;

    public InventoryOnboardingController(TeaOnlineShopContext context, StockLedgerService stockLedger)
    {
        _context = context;
        _stockLedger = stockLedger;
    }

    public async Task<IActionResult> Index()
    {
        var batches = await _context.InventoryImportBatches
            .AsNoTracking()
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Take(100)
            .ToListAsync();
        return View(batches);
    }

    [HttpGet]
    public IActionResult Upload() => View(new InventoryImportUploadViewModel());

    [HttpGet]
    public IActionResult Template()
    {
        var csv = string.Join(',', RequiredHeaders) + Environment.NewLine +
                  "Tea,TEA-1001,125.5,85.00,MAIN,DEFAULT,Verified opening count" + Environment.NewLine +
                  "Supply,SUPITEM-1001,40,,MAIN,DEFAULT,Verified opening count";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "inventory-opening-balance-template.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(InventoryImportUploadViewModel model)
    {
        if (model.File is null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Select a non-empty CSV file.");
        }
        else
        {
            if (model.File.Length > MaximumFileSize)
                ModelState.AddModelError(nameof(model.File), "The CSV file cannot exceed 2 MB.");
            if (!string.Equals(Path.GetExtension(model.File.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.File), "Only .csv files are accepted.");
        }

        if (!ModelState.IsValid)
            return View(model);

        await using var memory = new MemoryStream();
        await model.File!.CopyToAsync(memory);
        var bytes = memory.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (await _context.InventoryImportBatches.AnyAsync(x => x.FileSha256 == hash))
        {
            ModelState.AddModelError(nameof(model.File), "This exact file has already been submitted. Open the existing batch instead.");
            return View(model);
        }

        var actorId = GetActorId();
        var batch = new InventoryImportBatch
        {
            Id = Guid.NewGuid(),
            ImportType = "OpeningBalance",
            FileName = Path.GetFileName(model.File.FileName),
            FileSha256 = hash,
            Status = "Validating",
            SubmittedByUserId = actorId,
            SubmittedByName = User.Identity?.Name ?? "Unknown administrator",
            SubmittedAtUtc = DateTime.UtcNow,
            Notes = model.Notes
        };

        ParseCsv(batch, bytes);
        await ValidateRowsAsync(batch);
        batch.TotalRows = batch.Rows.Count;
        batch.RejectedRows = batch.Errors.Select(x => x.RowNumber).Distinct().Count();
        batch.ValidRows = batch.TotalRows - batch.RejectedRows;
        batch.Status = batch.Errors.Count == 0 ? "PendingApproval" : "RejectedValidation";

        _context.InventoryImportBatches.Add(batch);
        await _context.SaveChangesAsync();
        TempData[batch.Errors.Count == 0 ? "SuccessMessage" : "ErrorMessage"] = batch.Errors.Count == 0
            ? "File validated. A different authorized administrator must approve it before stock changes."
            : "The file was rejected. Review and correct every validation error before resubmitting.";
        return RedirectToAction(nameof(Details), new { id = batch.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var batch = await _context.InventoryImportBatches
            .AsNoTracking()
            .Include(x => x.Rows.OrderBy(r => r.RowNumber))
            .Include(x => x.Errors.OrderBy(e => e.RowNumber))
            .SingleOrDefaultAsync(x => x.Id == id);
        return batch is null ? NotFound() : View(batch);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var actorId = GetActorId();
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var batch = await _context.InventoryImportBatches
            .Include(x => x.Rows)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (batch is null)
            return NotFound();
        if (batch.Status != "PendingApproval")
        {
            TempData["ErrorMessage"] = "Only a validated pending batch can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (batch.SubmittedByUserId == actorId)
        {
            TempData["ErrorMessage"] = "Segregation of duties is enforced: the submitter cannot approve the same opening-balance batch.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            foreach (var row in batch.Rows.OrderBy(x => x.RowNumber))
            {
                var warehouse = await _context.Warehouses.SingleAsync(x => x.Code == row.WarehouseCode && x.IsActive);
                var bin = await _context.WarehouseBins.SingleAsync(x => x.WarehouseId == warehouse.Id && x.Code == row.BinCode && x.IsActive);
                StockLedgerEntry ledger;
                var request = new StockMovementRequest
                {
                    MovementType = "OpeningBalance",
                    QuantityChange = row.Quantity,
                    ReferenceType = "ImportBatch",
                    ReferenceNumber = batch.Id.ToString("D"),
                    Reason = row.Reason,
                    PerformedByUserId = actorId,
                    PerformedByName = User.Identity?.Name ?? "Unknown administrator",
                    UnitCost = row.UnitCost,
                    WarehouseId = warehouse.Id,
                    BinId = bin.Id,
                    CorrelationId = batch.Id
                };

                if (row.ItemType == "Tea")
                {
                    var itemId = await _context.TeaInventoryItems
                        .Where(x => x.ItemCode == row.ItemCode)
                        .Select(x => x.Id)
                        .SingleAsync();
                    await EnsureNoPriorMovementAsync(itemId, null, row.ItemCode);
                    ledger = await _stockLedger.RecordTeaMovementAsync(itemId, request);
                }
                else
                {
                    var itemId = await _context.SupplyItems
                        .Where(x => x.ItemCode == row.ItemCode)
                        .Select(x => x.Id)
                        .SingleAsync();
                    await EnsureNoPriorMovementAsync(null, itemId, row.ItemCode);
                    ledger = await _stockLedger.RecordSupplyMovementAsync(itemId, request);
                }

                row.LedgerEntryId = ledger.Id;
                row.Status = "Applied";
            }

            batch.Status = "Approved";
            batch.ApprovedByUserId = actorId;
            batch.ApprovedByName = User.Identity?.Name ?? "Unknown administrator";
            batch.ApprovedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Opening balances were approved and posted to the immutable stock ledger.";
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
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var batch = await _context.InventoryImportBatches.SingleOrDefaultAsync(x => x.Id == id);
        if (batch is null)
            return NotFound();
        if (batch.Status != "PendingApproval")
        {
            TempData["ErrorMessage"] = "Only a pending batch can be rejected.";
        }
        else if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "A rejection reason is required.";
        }
        else
        {
            batch.Status = "RejectedApproval";
            batch.Notes = string.IsNullOrWhiteSpace(batch.Notes)
                ? $"Rejected: {reason.Trim()}"
                : $"{batch.Notes}{Environment.NewLine}Rejected: {reason.Trim()}";
            batch.ApprovedByUserId = GetActorId();
            batch.ApprovedByName = User.Identity?.Name;
            batch.ApprovedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "The batch was rejected; no stock was changed.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private static void ParseCsv(InventoryImportBatch batch, byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var parser = new TextFieldParser(stream)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            };
            parser.SetDelimiters(",");
            if (parser.EndOfData)
            {
                AddError(batch, 1, "Header", "EMPTY_FILE", "The CSV file is empty.");
                return;
            }

            var headers = parser.ReadFields() ?? Array.Empty<string>();
            if (headers.Length != RequiredHeaders.Length ||
                !headers.Select(x => x.Trim()).SequenceEqual(RequiredHeaders, StringComparer.OrdinalIgnoreCase))
            {
                AddError(batch, 1, "Header", "INVALID_HEADER",
                    "Required columns, in order: " + string.Join(", ", RequiredHeaders));
                return;
            }

            var rowNumber = 1;
            while (!parser.EndOfData)
            {
                rowNumber++;
                string[]? fields;
                try
                {
                    fields = parser.ReadFields();
                }
                catch (MalformedLineException ex)
                {
                    AddError(batch, rowNumber, "Row", "MALFORMED_CSV", ex.Message);
                    continue;
                }

                if (fields is null || fields.All(string.IsNullOrWhiteSpace))
                    continue;
                if (fields.Length != RequiredHeaders.Length)
                {
                    AddError(batch, rowNumber, "Row", "COLUMN_COUNT", $"Expected {RequiredHeaders.Length} columns but found {fields.Length}.");
                    continue;
                }

                var itemType = NormalizeItemType(fields[0]);
                var quantityValid = decimal.TryParse(fields[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity);
                var costValid = string.IsNullOrWhiteSpace(fields[3]) ||
                                decimal.TryParse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture, out _);
                decimal? unitCost = null;
                if (!string.IsNullOrWhiteSpace(fields[3]) && costValid)
                    unitCost = decimal.Parse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture);

                batch.Rows.Add(new InventoryImportRow
                {
                    RowNumber = rowNumber,
                    ItemType = itemType,
                    ItemCode = fields[1].Trim().ToUpperInvariant(),
                    Quantity = quantityValid ? quantity : 0,
                    UnitCost = unitCost,
                    WarehouseCode = string.IsNullOrWhiteSpace(fields[4]) ? "MAIN" : fields[4].Trim().ToUpperInvariant(),
                    BinCode = string.IsNullOrWhiteSpace(fields[5]) ? "DEFAULT" : fields[5].Trim().ToUpperInvariant(),
                    Reason = fields[6].Trim(),
                    Status = "Validated"
                });
                if (!quantityValid)
                    AddError(batch, rowNumber, "Quantity", "INVALID_NUMBER", "Quantity must be a decimal number using a dot as the decimal separator.");
                if (!costValid)
                    AddError(batch, rowNumber, "UnitCost", "INVALID_NUMBER", "UnitCost must be blank or a valid decimal number.");
            }
        }
        catch (Exception ex)
        {
            AddError(batch, 1, "File", "READ_ERROR", ex.Message);
        }
    }

    private async Task ValidateRowsAsync(InventoryImportBatch batch)
    {
        foreach (var duplicate in batch.Rows.GroupBy(x => x.ItemCode, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
        {
            foreach (var row in duplicate)
                AddError(batch, row.RowNumber, "ItemCode", "DUPLICATE_ITEM", "Each item may appear only once in an opening-balance file.");
        }

        foreach (var row in batch.Rows)
        {
            if (row.ItemType is not ("Tea" or "Supply"))
                AddError(batch, row.RowNumber, "ItemType", "INVALID_ITEM_TYPE", "ItemType must be Tea or Supply.");
            if (string.IsNullOrWhiteSpace(row.ItemCode))
                AddError(batch, row.RowNumber, "ItemCode", "REQUIRED", "ItemCode is required.");
            if (row.Quantity <= 0)
                AddError(batch, row.RowNumber, "Quantity", "OUT_OF_RANGE", "Opening quantity must be greater than zero.");
            if (row.UnitCost < 0)
                AddError(batch, row.RowNumber, "UnitCost", "OUT_OF_RANGE", "Unit cost cannot be negative.");
            if (string.IsNullOrWhiteSpace(row.Reason))
                AddError(batch, row.RowNumber, "Reason", "REQUIRED", "A verified count reason is required.");

            var warehouse = await _context.Warehouses.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Code == row.WarehouseCode && x.IsActive);
            if (warehouse is null)
            {
                AddError(batch, row.RowNumber, "WarehouseCode", "NOT_FOUND", "The active warehouse code does not exist.");
                continue;
            }
            if (!await _context.WarehouseBins.AsNoTracking()
                    .AnyAsync(x => x.WarehouseId == warehouse.Id && x.Code == row.BinCode && x.IsActive))
                AddError(batch, row.RowNumber, "BinCode", "NOT_FOUND", "The active bin code does not exist in this warehouse.");

            if (row.ItemType == "Tea")
            {
                var itemId = await _context.TeaInventoryItems.AsNoTracking()
                    .Where(x => x.ItemCode == row.ItemCode).Select(x => (int?)x.Id).SingleOrDefaultAsync();
                if (!itemId.HasValue)
                    AddError(batch, row.RowNumber, "ItemCode", "NOT_FOUND", "No tea inventory item has this code.");
                else if (await _context.StockLedgerEntries.AsNoTracking().AnyAsync(x => x.InventoryItemId == itemId.Value))
                    AddError(batch, row.RowNumber, "ItemCode", "ALREADY_OPENED", "This item already has ledger history; use a controlled adjustment, not an opening balance.");
            }
            else if (row.ItemType == "Supply")
            {
                var itemId = await _context.SupplyItems.AsNoTracking()
                    .Where(x => x.ItemCode == row.ItemCode).Select(x => (int?)x.Id).SingleOrDefaultAsync();
                if (!itemId.HasValue)
                    AddError(batch, row.RowNumber, "ItemCode", "NOT_FOUND", "No supply item has this code.");
                else if (await _context.StockLedgerEntries.AsNoTracking().AnyAsync(x => x.SupplyItemId == itemId.Value))
                    AddError(batch, row.RowNumber, "ItemCode", "ALREADY_OPENED", "This item already has ledger history; use a controlled adjustment, not an opening balance.");
            }
        }
    }

    private async Task EnsureNoPriorMovementAsync(int? inventoryItemId, int? supplyItemId, string itemCode)
    {
        var exists = inventoryItemId.HasValue
            ? await _context.StockLedgerEntries.AnyAsync(x => x.InventoryItemId == inventoryItemId.Value)
            : await _context.StockLedgerEntries.AnyAsync(x => x.SupplyItemId == supplyItemId!.Value);
        if (exists)
            throw new InvalidOperationException($"{itemCode} acquired ledger history after validation and cannot receive another opening balance.");
    }

    private int GetActorId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("The authenticated administrator has no valid user identifier.");

    private static string NormalizeItemType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "tea" or "inventory" or "teainventory" => "Tea",
        "supply" or "supplyitem" => "Supply",
        _ => value.Trim()
    };

    private static void AddError(InventoryImportBatch batch, int row, string field, string code, string message) =>
        batch.Errors.Add(new InventoryImportRowError
        {
            BatchId = batch.Id,
            RowNumber = row,
            FieldName = field,
            ErrorCode = code,
            Message = message
        });
}
