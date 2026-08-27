using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Models.Dbase;

namespace TeaOnlineShop.Services
{
    public class InventoryService
    {
        private readonly TeaOnlineShopContext _context;
        private readonly QRCodeService _qrCodeService;
        private readonly StockLedgerService _stockLedger;

        public InventoryService(
            TeaOnlineShopContext context,
            QRCodeService qrCodeService,
            StockLedgerService stockLedger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _stockLedger = stockLedger;
        }

        public async Task<TeaInventoryItem?> GetItemByQRCodeAsync(string qrCode)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCode))
                    return null;

                var item = await _context.TeaInventoryItems
                    .FirstOrDefaultAsync(i => i.QRCodeData == qrCode);
                    
                if (item != null)
                {
                    // Ensure no null string values
                    item.Name ??= string.Empty;
                    item.TeaType ??= string.Empty;
                    item.Grade ??= string.Empty;
                    item.Origin ??= string.Empty;
                    item.HarvestSeason ??= string.Empty;
                    item.BatchNumber ??= string.Empty;
                    item.Description ??= string.Empty;
                    item.Unit ??= string.Empty;
                    item.Status ??= string.Empty;
                    item.QRCodeData ??= string.Empty;
                    item.LastCorrectedBy ??= string.Empty;
                    item.CorrectionReason ??= string.Empty;
                }
                
                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetItemByQRCodeAsync: {ex}");
                return null;
            }
        }

        public async Task<TeaInventoryItem?> GetItemByIdAsync(int id)
        {
            try
            {
                var item = await _context.TeaInventoryItems
                    .FirstOrDefaultAsync(i => i.Id == id);
                    
                if (item != null)
                {
                    // Ensure no null string values
                    item.Name ??= string.Empty;
                    item.TeaType ??= string.Empty;
                    item.Grade ??= string.Empty;
                    item.Origin ??= string.Empty;
                    item.HarvestSeason ??= string.Empty;
                    item.BatchNumber ??= string.Empty;
                    item.Description ??= string.Empty;
                    item.Unit ??= string.Empty;
                    item.Status ??= string.Empty;
                    item.QRCodeData ??= string.Empty;
                    item.LastCorrectedBy ??= string.Empty;
                    item.CorrectionReason ??= string.Empty;
                }
                
                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetItemByIdAsync: {ex}");
                return null;
            }
        }

        public async Task<List<TeaInventoryItem>> GetItemsByTypeAndGradeAsync(string teaType, string grade)
        {
            try
            {
                var query = _context.TeaInventoryItems.AsQueryable();

                if (!string.IsNullOrEmpty(teaType))
                    query = query.Where(i => i.TeaType == teaType);

                if (!string.IsNullOrEmpty(grade))
                    query = query.Where(i => i.Grade == grade);

                var items = await query.ToListAsync();
                
                // Ensure no null string values
                foreach (var item in items)
                {
                    item.Name ??= string.Empty;
                    item.TeaType ??= string.Empty;
                    item.Grade ??= string.Empty;
                    item.Origin ??= string.Empty;
                    item.HarvestSeason ??= string.Empty;
                    item.BatchNumber ??= string.Empty;
                    item.Description ??= string.Empty;
                    item.Unit ??= string.Empty;
                    item.Status ??= string.Empty;
                    item.QRCodeData ??= string.Empty;
                    item.LastCorrectedBy ??= string.Empty;
                    item.CorrectionReason ??= string.Empty;
                }
                
                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetItemsByTypeAndGradeAsync: {ex}");
                // Return empty list instead of throwing to avoid breaking the UI
                return new List<TeaInventoryItem>();
            }
        }

        public async Task<TeaInventoryItem> CreateInventoryItemAsync(TeaInventoryItem model, string username)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                model.ItemCode = (model.ItemCode ?? string.Empty).Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(model.ItemCode) &&
                    await _context.TeaInventoryItems.AnyAsync(x => x.ItemCode == model.ItemCode))
                {
                    throw new InvalidOperationException($"Inventory item code '{model.ItemCode}' already exists.");
                }

                // Ensure required fields are set
                if (string.IsNullOrEmpty(model.QRCodeData))
                {
                    var codeForQr = string.IsNullOrWhiteSpace(model.ItemCode) ? "PENDING" : model.ItemCode;
                    model.QRCodeData = $"INV-{codeForQr}-{Guid.NewGuid():N}";
                }
                
                model.CreatedDate = DateTime.Now;
                model.HasBeenCorrected = false;
                var openingQuantity = model.CurrentStock;
                model.CurrentStock = 0;

                var generatedItemCode = string.IsNullOrWhiteSpace(model.ItemCode);
                if (generatedItemCode)
                {
                    model.ItemCode = $"PENDING-{Guid.NewGuid():N}";
                }
                
                // Ensure nullable string properties are not null but empty string if needed
                model.LastCorrectedBy ??= string.Empty;
                model.CorrectionReason ??= string.Empty;
                model.Origin ??= string.Empty;
                model.HarvestSeason ??= string.Empty;
                model.BatchNumber ??= string.Empty;
                model.Description ??= string.Empty;
                
                // Add the inventory item to the database
                _context.TeaInventoryItems.Add(model);
                
                // Save the inventory item first to get its ID
                await _context.SaveChangesAsync();

                if (generatedItemCode)
                {
                    model.ItemCode = $"TEA-{model.Id}";
                    await _context.SaveChangesAsync();
                }

                if (openingQuantity > 0)
                {
                    await _stockLedger.RecordTeaMovementAsync(model.Id, new StockMovementRequest
                    {
                        MovementType = "OpeningBalance",
                        QuantityChange = openingQuantity,
                        ReferenceType = "InventoryOnboarding",
                        ReferenceId = model.Id,
                        ReferenceNumber = model.ItemCode,
                        Reason = "Opening balance recorded when the inventory item was created.",
                        PerformedByName = string.IsNullOrWhiteSpace(username) ? "Unknown staff user" : username,
                        UnitCost = model.UnitCost
                    });
                }

                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine($"Created inventory item with ID: {model.Id}");
                return model;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"Error in CreateInventoryItemAsync: {ex}");
                throw; // Re-throw for controller to handle
            }
        }

        public async Task<bool> UpdateStockAsync(int itemId, decimal quantity, string transactionType, 
            string userName, string notes, int? referenceId = null, string? referenceNumber = null, 
            decimal? unitPrice = null, string? qrCodeScanned = null)
        {
            if (quantity <= 0 || !await _context.TeaInventoryItems.AnyAsync(x => x.Id == itemId))
                return false;

            var additions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Delivery", "Production", "Return", "Correction", "OpeningBalance"
            };
            var reductions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Sale", "Damage", "Transfer", "StockOut"
            };

            if (!additions.Contains(transactionType) && !reductions.Contains(transactionType))
                return false;

            try
            {
                await _stockLedger.RecordTeaMovementAsync(itemId, new StockMovementRequest
                {
                    MovementType = transactionType,
                    QuantityChange = additions.Contains(transactionType) ? quantity : -quantity,
                    ReferenceType = referenceId.HasValue ? "OperationalRecord" : "ManualInventory",
                    ReferenceId = referenceId,
                    ReferenceNumber = referenceNumber ?? qrCodeScanned ?? string.Empty,
                    Reason = string.IsNullOrWhiteSpace(notes)
                        ? $"{transactionType} stock movement."
                        : notes,
                    PerformedByName = string.IsNullOrWhiteSpace(userName) ? "Unknown staff user" : userName,
                    UnitCost = unitPrice
                });
                return true;
            }
            catch (InsufficientStockException)
            {
                return false;
            }
        }
        
        public async Task<bool> AdjustStockAsync(int itemId, decimal newQuantity, string reason, string userName)
        {
            var item = await _context.TeaInventoryItems.FindAsync(itemId);
            if (item == null)
                return false;
                
            if (newQuantity < 0 || string.IsNullOrWhiteSpace(reason))
                return false;

            var previousStock = item.CurrentStock;
            if (newQuantity != previousStock)
            {
                await _stockLedger.SetTeaStockAsync(itemId, newQuantity, new StockMovementRequest
                {
                    MovementType = "StockAdjustment",
                    ReferenceType = "ManualInventory",
                    ReferenceNumber = item.ItemCode,
                    Reason = reason,
                    PerformedByName = string.IsNullOrWhiteSpace(userName) ? "Unknown staff user" : userName,
                    UnitCost = item.UnitCost
                });
            }

            item.HasBeenCorrected = true;
            item.LastCorrectionDate = DateTime.Now;
            item.LastCorrectedBy = userName;
            item.CorrectionReason = reason;
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<List<TeaInventoryTransaction>> GetTransactionHistoryAsync(int itemId, int count = 10)
        {
            try
            {
                // Use projection to handle NULL values safely
                var transactions = await _context.TeaInventoryTransactions
                    .Where(t => t.InventoryItemId == itemId)
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(count)
                    .Select(t => new TeaInventoryTransaction
                    {
                        Id = t.Id,
                        InventoryItemId = t.InventoryItemId,
                        TransactionDate = t.TransactionDate,
                        TransactionType = t.TransactionType ?? string.Empty,
                        Quantity = t.Quantity,
                        PreviousStock = t.PreviousStock,
                        NewStock = t.NewStock,
                        ReferenceNumber = t.ReferenceNumber ?? string.Empty,
                        UnitPrice = t.UnitPrice,
                        Notes = t.Notes ?? string.Empty,
                        PerformedBy = t.PerformedBy ?? string.Empty,
                        IsCorrection = t.IsCorrection,
                        CorrectionReason = t.CorrectionReason ?? string.Empty,
                        QRCodeScanned = t.QRCodeScanned ?? string.Empty,
                        RelatedTransactionId = t.RelatedTransactionId,
                        ReferenceId = t.ReferenceId
                    })
                    .ToListAsync();
                    
                return transactions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetTransactionHistoryAsync: {ex}");
                // Return empty list instead of throwing to avoid breaking the UI
                return new List<TeaInventoryTransaction>();
            }
        }
        
        public async Task<List<TeaInventoryItem>> GetLowStockItemsAsync()
        {
            try
            {
                var items = await _context.TeaInventoryItems
                    .Where(i => i.CurrentStock <= i.MinimumStock && i.Status == "Active")
                    .OrderBy(i => i.TeaType)
                    .ThenBy(i => i.Grade)
                    .ToListAsync();
                    
                // Ensure no null string values
                foreach (var item in items)
                {
                    item.Name ??= string.Empty;
                    item.TeaType ??= string.Empty;
                    item.Grade ??= string.Empty;
                    item.Origin ??= string.Empty;
                    item.HarvestSeason ??= string.Empty;
                    item.BatchNumber ??= string.Empty;
                    item.Description ??= string.Empty;
                    item.Unit ??= string.Empty;
                    item.Status ??= string.Empty;
                    item.QRCodeData ??= string.Empty;
                    item.LastCorrectedBy ??= string.Empty;
                    item.CorrectionReason ??= string.Empty;
                }
                
                return items;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetLowStockItemsAsync: {ex}");
                return new List<TeaInventoryItem>();
            }
        }
    }
} 
