using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Services;

const string connectionString = "Server=localhost\\SQLEXPRESS;Database=TeaOnlineShop;Trusted_Connection=True;TrustServerCertificate=true";
var options = new DbContextOptionsBuilder<TeaOnlineShopContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var context = new TeaOnlineShopContext(options);
var service = new StockLedgerService(context);
var product = await context.Products.AsNoTracking()
    .Include(x => x.InventoryMapping)
    .OrderBy(x => x.Id)
    .FirstAsync(x => x.InventoryMapping != null && x.Quantity > 0);
var mapping = product.InventoryMapping!;
var openingBalance = await context.StockBalances.AsNoTracking()
    .Where(x => x.InventoryItemId == mapping.InventoryItemId)
    .SumAsync(x => x.Quantity);
var openingLedgerCount = await context.StockLedgerEntries.CountAsync();
var openingProductQuantity = product.Quantity;

await using (var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
{
    var entry = await service.RecordProductSaleAsync(product.Id, 1m, new StockMovementRequest
    {
        MovementType = "IntegrationVerification",
        ReferenceType = "RollbackTest",
        ReferenceNumber = $"VERIFY-{Guid.NewGuid():N}",
        Reason = "Rollback-only verification of atomic inventory posting.",
        PerformedByName = "Automated integration check"
    });

    var changedBalance = await context.StockBalances.AsNoTracking()
        .Where(x => x.InventoryItemId == mapping.InventoryItemId)
        .SumAsync(x => x.Quantity);
    if (changedBalance != openingBalance - mapping.QuantityPerUnit)
        throw new InvalidOperationException("Warehouse balance was not reduced by the mapped quantity.");
    if (entry.NewStock != entry.PreviousStock + entry.QuantityChange)
        throw new InvalidOperationException("Ledger balance proof is invalid.");
    if (await context.StockLedgerEntries.CountAsync() != openingLedgerCount + 1)
        throw new InvalidOperationException("Ledger entry was not created atomically.");
    var changedProductQuantity = await context.Products.Where(x => x.Id == product.Id).Select(x => x.Quantity).SingleAsync();
    if (changedProductQuantity != openingProductQuantity - 1)
        throw new InvalidOperationException("Product availability was not synchronized.");

    var oversellRejected = false;
    try
    {
        await service.RecordProductSaleAsync(product.Id, 1_000_000m, new StockMovementRequest
        {
            MovementType = "IntegrationVerification",
            ReferenceType = "RollbackTest",
            ReferenceNumber = "VERIFY-OVERSELL",
            Reason = "Confirm that negative inventory is rejected.",
            PerformedByName = "Automated integration check"
        });
    }
    catch (InsufficientStockException)
    {
        oversellRejected = true;
    }
    if (!oversellRejected)
        throw new InvalidOperationException("Overselling was not rejected.");

    await transaction.RollbackAsync();
}

context.ChangeTracker.Clear();
var restoredBalance = await context.StockBalances.AsNoTracking()
    .Where(x => x.InventoryItemId == mapping.InventoryItemId)
    .SumAsync(x => x.Quantity);
var restoredProductQuantity = await context.Products.Where(x => x.Id == product.Id).Select(x => x.Quantity).SingleAsync();
var restoredLedgerCount = await context.StockLedgerEntries.CountAsync();
if (restoredBalance != openingBalance || restoredProductQuantity != openingProductQuantity || restoredLedgerCount != openingLedgerCount)
    throw new InvalidOperationException("Rollback verification changed persistent business data.");

var immutableTriggerExists = await context.Database
    .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE [name] = 'TR_StockLedgerEntry_Immutable'")
    .SingleAsync() == 1;
if (!immutableTriggerExists)
    throw new InvalidOperationException("The database immutability trigger is missing.");

Console.WriteLine("PASS: atomic movement, mapped product sync, oversell protection, rollback safety, and immutable-ledger trigger verified.");
