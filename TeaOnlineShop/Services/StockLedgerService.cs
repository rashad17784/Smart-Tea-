using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Services;

public sealed class StockMovementRequest
{
    public string MovementType { get; init; } = string.Empty;
    public decimal QuantityChange { get; init; }
    public string ReferenceType { get; init; } = "Manual";
    public int? ReferenceId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int? PerformedByUserId { get; init; }
    public string PerformedByName { get; init; } = string.Empty;
    public decimal? UnitCost { get; init; }
    public int? WarehouseId { get; init; }
    public int? BinId { get; init; }
    public Guid? CorrelationId { get; init; }
    public bool IsReversal { get; init; }
    public long? ReversesEntryId { get; init; }
}

public sealed class InsufficientStockException : InvalidOperationException
{
    public InsufficientStockException(string itemCode, decimal requested, decimal available)
        : base($"Insufficient stock for {itemCode}. Requested {requested:0.####}; available {available:0.####}.")
    {
        ItemCode = itemCode;
        Requested = requested;
        Available = available;
    }

    public string ItemCode { get; }
    public decimal Requested { get; }
    public decimal Available { get; }
}

/// <summary>
/// The sole write path for warehouse stock. It updates the location balance,
/// aggregate item quantity, legacy reporting transaction and immutable ledger
/// in one serializable database transaction.
/// </summary>
public sealed class StockLedgerService
{
    private readonly TeaOnlineShopContext _context;

    public StockLedgerService(TeaOnlineShopContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetAvailableProductUnitsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var mapping = await _context.ProductInventoryMappings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId && x.IsActive, cancellationToken);

        if (mapping is null || mapping.QuantityPerUnit <= 0)
        {
            return 0;
        }

        var quantity = await _context.StockBalances
            .AsNoTracking()
            .Where(x => x.InventoryItemId == mapping.InventoryItemId)
            .SumAsync(x => (decimal?)x.Quantity, cancellationToken) ?? 0m;

        return Math.Floor(quantity / mapping.QuantityPerUnit);
    }

    public async Task<StockLedgerEntry> RecordProductSaleAsync(
        int productId,
        decimal units,
        StockMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (units <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(units), "Sale quantity must be greater than zero.");
        }

        var mapping = await _context.ProductInventoryMappings
            .SingleOrDefaultAsync(x => x.ProductId == productId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Product {productId} is not mapped to a warehouse inventory item.");

        return await RecordTeaMovementAsync(
            mapping.InventoryItemId,
            request.withQuantityChange(-(units * mapping.QuantityPerUnit)),
            cancellationToken);
    }

    public Task<StockLedgerEntry> RecordTeaMovementAsync(
        int inventoryItemId,
        StockMovementRequest request,
        CancellationToken cancellationToken = default) =>
        RecordMovementAsync(inventoryItemId, null, request, cancellationToken);

    public Task<StockLedgerEntry> RecordSupplyMovementAsync(
        int supplyItemId,
        StockMovementRequest request,
        CancellationToken cancellationToken = default) =>
        RecordMovementAsync(null, supplyItemId, request, cancellationToken);

    public async Task<StockLedgerEntry> SetTeaStockAsync(
        int inventoryItemId,
        decimal targetQuantity,
        StockMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (targetQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetQuantity), "Stock cannot be negative.");
        }

        var ownsTransaction = _context.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var (warehouseId, binId) = await ResolveLocationAsync(request, cancellationToken);
            var currentTotal = await _context.StockBalances
                .FromSqlInterpolated($@"
SELECT * FROM [dbo].[StockBalance] WITH (UPDLOCK, HOLDLOCK)
WHERE [InventoryItemId] = {inventoryItemId}")
                .SumAsync(x => x.Quantity, cancellationToken);
            var change = targetQuantity - currentTotal;
            if (change == 0)
                throw new InvalidOperationException("The requested stock quantity already matches the warehouse total.");

            var ledger = await RecordTeaMovementAsync(
                inventoryItemId,
                request.withQuantityChange(change).withLocation(warehouseId, binId),
                cancellationToken);
            if (ownsTransaction && transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return ledger;
        }
        catch
        {
            if (ownsTransaction && transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<StockLedgerEntry> ReverseAsync(
        long entryId,
        int? performedByUserId,
        string performedByName,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var original = await _context.StockLedgerEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == entryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Ledger entry {entryId} was not found.");

        var alreadyReversed = await _context.StockLedgerEntries
            .AsNoTracking()
            .AnyAsync(x => x.ReversesEntryId == entryId, cancellationToken);
        if (alreadyReversed)
        {
            throw new InvalidOperationException("This ledger entry has already been reversed.");
        }

        var request = new StockMovementRequest
        {
            MovementType = "Reversal",
            QuantityChange = -original.QuantityChange,
            ReferenceType = "LedgerEntry",
            ReferenceId = original.Id > int.MaxValue ? null : (int)original.Id,
            ReferenceNumber = original.EntryNumber.ToString("D"),
            Reason = RequireText(reason, nameof(reason)),
            PerformedByUserId = performedByUserId,
            PerformedByName = performedByName,
            UnitCost = original.UnitCost,
            WarehouseId = original.WarehouseId,
            BinId = original.BinId,
            CorrelationId = original.CorrelationId,
            IsReversal = true,
            ReversesEntryId = original.Id
        };

        return original.InventoryItemId.HasValue
            ? await RecordTeaMovementAsync(original.InventoryItemId.Value, request, cancellationToken)
            : await RecordSupplyMovementAsync(original.SupplyItemId!.Value, request, cancellationToken);
    }

    private async Task<StockLedgerEntry> RecordMovementAsync(
        int? inventoryItemId,
        int? supplyItemId,
        StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(inventoryItemId, supplyItemId, request);

        var ownsTransaction = _context.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;
        if (ownsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        }

        try
        {
            var (warehouseId, binId) = await ResolveLocationAsync(request, cancellationToken);
            string itemCode;
            string itemName;

            if (inventoryItemId.HasValue)
            {
                var item = await _context.TeaInventoryItems
                    .SingleOrDefaultAsync(x => x.Id == inventoryItemId.Value, cancellationToken)
                    ?? throw new KeyNotFoundException($"Inventory item {inventoryItemId.Value} was not found.");
                itemCode = item.ItemCode;
                itemName = item.Name;
                await EnsureInventoryBalanceAsync(warehouseId, binId, item.Id, cancellationToken);
            }
            else
            {
                var item = await _context.SupplyItems
                    .SingleOrDefaultAsync(x => x.Id == supplyItemId!.Value, cancellationToken)
                    ?? throw new KeyNotFoundException($"Supply item {supplyItemId.Value} was not found.");
                itemCode = item.ItemCode;
                itemName = item.Name;
                await EnsureSupplyBalanceAsync(warehouseId, binId, item.Id, cancellationToken);
            }

            var balance = inventoryItemId.HasValue
                ? await _context.StockBalances.FromSqlInterpolated($@"
SELECT * FROM [dbo].[StockBalance] WITH (UPDLOCK, HOLDLOCK)
WHERE [WarehouseId] = {warehouseId} AND [BinId] = {binId} AND [InventoryItemId] = {inventoryItemId.Value}")
                    .SingleAsync(cancellationToken)
                : await _context.StockBalances.FromSqlInterpolated($@"
SELECT * FROM [dbo].[StockBalance] WITH (UPDLOCK, HOLDLOCK)
WHERE [WarehouseId] = {warehouseId} AND [BinId] = {binId} AND [SupplyItemId] = {supplyItemId!.Value}")
                    .SingleAsync(cancellationToken);

            var previous = balance.Quantity;
            var next = previous + request.QuantityChange;
            if (next < 0)
            {
                throw new InsufficientStockException(itemCode, Math.Abs(request.QuantityChange), previous);
            }

            balance.Quantity = next;
            balance.LastUpdatedUtc = DateTime.UtcNow;

            var ledger = new StockLedgerEntry
            {
                EntryNumber = Guid.NewGuid(),
                CorrelationId = request.CorrelationId ?? Guid.NewGuid(),
                WarehouseId = warehouseId,
                BinId = binId,
                InventoryItemId = inventoryItemId,
                SupplyItemId = supplyItemId,
                ItemCode = itemCode,
                ItemName = itemName,
                MovementType = RequireText(request.MovementType, nameof(request.MovementType)),
                QuantityChange = request.QuantityChange,
                PreviousStock = previous,
                NewStock = next,
                UnitCost = request.UnitCost,
                ReferenceType = RequireText(request.ReferenceType, nameof(request.ReferenceType)),
                ReferenceId = request.ReferenceId,
                ReferenceNumber = request.ReferenceNumber?.Trim() ?? string.Empty,
                Reason = RequireText(request.Reason, nameof(request.Reason)),
                PerformedByUserId = request.PerformedByUserId,
                PerformedByName = RequireText(request.PerformedByName, nameof(request.PerformedByName)),
                OccurredAtUtc = DateTime.UtcNow,
                IsReversal = request.IsReversal,
                ReversesEntryId = request.ReversesEntryId
            };
            _context.StockLedgerEntries.Add(ledger);

            if (inventoryItemId.HasValue)
            {
                _context.TeaInventoryTransactions.Add(new TeaInventoryTransaction
                {
                    InventoryItemId = inventoryItemId.Value,
                    ReferenceId = request.ReferenceId,
                    TransactionDate = DateTime.Now,
                    TransactionType = ledger.MovementType,
                    Quantity = Math.Abs(request.QuantityChange),
                    PreviousStock = previous,
                    NewStock = next,
                    ReferenceNumber = ledger.ReferenceNumber,
                    UnitPrice = request.UnitCost,
                    Notes = ledger.Reason,
                    PerformedBy = ledger.PerformedByName,
                    IsCorrection = ledger.MovementType.Contains("Adjustment", StringComparison.OrdinalIgnoreCase),
                    CorrectionReason = ledger.Reason,
                    QRCodeScanned = string.Empty
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await SynchronizeAggregateQuantitiesAsync(inventoryItemId, supplyItemId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            if (ownsTransaction && transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return ledger;
        }
        catch
        {
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task SynchronizeAggregateQuantitiesAsync(
        int? inventoryItemId,
        int? supplyItemId,
        CancellationToken cancellationToken)
    {
        if (inventoryItemId.HasValue)
        {
            var total = await _context.StockBalances
                .Where(x => x.InventoryItemId == inventoryItemId.Value)
                .SumAsync(x => x.Quantity, cancellationToken);
            var inventoryItem = await _context.TeaInventoryItems
                .SingleAsync(x => x.Id == inventoryItemId.Value, cancellationToken);
            inventoryItem.CurrentStock = total;
            inventoryItem.LastUpdated = DateTime.Now;

            var mappings = await _context.ProductInventoryMappings
                .Include(x => x.Product)
                .Where(x => x.InventoryItemId == inventoryItemId.Value && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var mapping in mappings)
            {
                mapping.Product.Quantity = mapping.QuantityPerUnit <= 0
                    ? 0
                    : checked((int)Math.Min(int.MaxValue, Math.Floor(total / mapping.QuantityPerUnit)));
            }
        }
        else
        {
            var total = await _context.StockBalances
                .Where(x => x.SupplyItemId == supplyItemId!.Value)
                .SumAsync(x => x.Quantity, cancellationToken);
            var supplyItem = await _context.SupplyItems
                .SingleAsync(x => x.Id == supplyItemId.Value, cancellationToken);
            supplyItem.CurrentStock = total;
        }
    }

    private async Task<(int WarehouseId, int BinId)> ResolveLocationAsync(
        StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var warehouse = request.WarehouseId.HasValue
            ? await _context.Warehouses.SingleOrDefaultAsync(x => x.Id == request.WarehouseId.Value && x.IsActive, cancellationToken)
            : await _context.Warehouses.SingleOrDefaultAsync(x => x.Code == "MAIN" && x.IsActive, cancellationToken);
        warehouse ??= await _context.Warehouses.FirstOrDefaultAsync(x => x.IsActive, cancellationToken);
        if (warehouse is null)
        {
            throw new InvalidOperationException("No active warehouse is configured.");
        }

        var bin = request.BinId.HasValue
            ? await _context.WarehouseBins.SingleOrDefaultAsync(
                x => x.Id == request.BinId.Value && x.WarehouseId == warehouse.Id && x.IsActive,
                cancellationToken)
            : await _context.WarehouseBins.SingleOrDefaultAsync(
                x => x.WarehouseId == warehouse.Id && x.Code == "DEFAULT" && x.IsActive,
                cancellationToken);
        bin ??= await _context.WarehouseBins.FirstOrDefaultAsync(
            x => x.WarehouseId == warehouse.Id && x.IsActive,
            cancellationToken);
        if (bin is null)
        {
            throw new InvalidOperationException($"Warehouse {warehouse.Code} has no active storage bin.");
        }

        return (warehouse.Id, bin.Id);
    }

    private async Task EnsureInventoryBalanceAsync(int warehouseId, int binId, int inventoryItemId, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS
(
    SELECT 1 FROM [dbo].[StockBalance] WITH (UPDLOCK, HOLDLOCK)
    WHERE [WarehouseId] = {warehouseId} AND [BinId] = {binId} AND [InventoryItemId] = {inventoryItemId}
)
BEGIN
    INSERT INTO [dbo].[StockBalance]
        ([WarehouseId], [BinId], [InventoryItemId], [SupplyItemId], [Quantity], [LastUpdatedUtc])
    VALUES ({warehouseId}, {binId}, {inventoryItemId}, NULL, 0, SYSUTCDATETIME());
END", cancellationToken);
    }

    private async Task EnsureSupplyBalanceAsync(int warehouseId, int binId, int supplyItemId, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS
(
    SELECT 1 FROM [dbo].[StockBalance] WITH (UPDLOCK, HOLDLOCK)
    WHERE [WarehouseId] = {warehouseId} AND [BinId] = {binId} AND [SupplyItemId] = {supplyItemId}
)
BEGIN
    INSERT INTO [dbo].[StockBalance]
        ([WarehouseId], [BinId], [InventoryItemId], [SupplyItemId], [Quantity], [LastUpdatedUtc])
    VALUES ({warehouseId}, {binId}, NULL, {supplyItemId}, 0, SYSUTCDATETIME());
END", cancellationToken);
    }

    private static void ValidateRequest(int? inventoryItemId, int? supplyItemId, StockMovementRequest request)
    {
        if (inventoryItemId.HasValue == supplyItemId.HasValue)
        {
            throw new ArgumentException("A stock movement must target exactly one inventory or supply item.");
        }
        if (request.QuantityChange == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.QuantityChange), "Stock movement cannot be zero.");
        }
        RequireText(request.MovementType, nameof(request.MovementType));
        RequireText(request.ReferenceType, nameof(request.ReferenceType));
        RequireText(request.Reason, nameof(request.Reason));
        RequireText(request.PerformedByName, nameof(request.PerformedByName));
    }

    private static string RequireText(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }
        return value.Trim();
    }
}

internal static class StockMovementRequestExtensions
{
    public static StockMovementRequest withQuantityChange(this StockMovementRequest request, decimal quantityChange) => new()
    {
        MovementType = request.MovementType,
        QuantityChange = quantityChange,
        ReferenceType = request.ReferenceType,
        ReferenceId = request.ReferenceId,
        ReferenceNumber = request.ReferenceNumber,
        Reason = request.Reason,
        PerformedByUserId = request.PerformedByUserId,
        PerformedByName = request.PerformedByName,
        UnitCost = request.UnitCost,
        WarehouseId = request.WarehouseId,
        BinId = request.BinId,
        CorrelationId = request.CorrelationId,
        IsReversal = request.IsReversal,
        ReversesEntryId = request.ReversesEntryId
    };

    public static StockMovementRequest withLocation(this StockMovementRequest request, int warehouseId, int binId) => new()
    {
        MovementType = request.MovementType,
        QuantityChange = request.QuantityChange,
        ReferenceType = request.ReferenceType,
        ReferenceId = request.ReferenceId,
        ReferenceNumber = request.ReferenceNumber,
        Reason = request.Reason,
        PerformedByUserId = request.PerformedByUserId,
        PerformedByName = request.PerformedByName,
        UnitCost = request.UnitCost,
        WarehouseId = warehouseId,
        BinId = binId,
        CorrelationId = request.CorrelationId,
        IsReversal = request.IsReversal,
        ReversesEntryId = request.ReversesEntryId
    };
}
