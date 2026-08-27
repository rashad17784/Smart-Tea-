using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaOnlineShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustrialInventoryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
SET XACT_ABORT ON;

IF OBJECT_ID(N'[dbo].[QRCodeScan]', N'U') IS NULL AND OBJECT_ID(N'[dbo].[QRCodeScans]', N'U') IS NOT NULL
    EXEC sp_rename N'[dbo].[QRCodeScans]', N'QRCodeScan';

IF COL_LENGTH('dbo.TeaInventoryItems', 'ItemCode') IS NULL
    ALTER TABLE [dbo].[TeaInventoryItems] ADD [ItemCode] nvarchar(50) NULL;

IF COL_LENGTH('dbo.SupplyItem', 'ItemCode') IS NULL
    ALTER TABLE [dbo].[SupplyItem] ADD [ItemCode] nvarchar(50) NULL;
""");

            migrationBuilder.Sql("""
SET XACT_ABORT ON;

UPDATE [dbo].[TeaInventoryItems]
SET [ItemCode] = CONCAT('TEA-', [Id])
WHERE NULLIF(LTRIM(RTRIM([ItemCode])), '') IS NULL;
ALTER TABLE [dbo].[TeaInventoryItems] ALTER COLUMN [ItemCode] nvarchar(50) NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TeaInventoryItems_ItemCode')
    CREATE UNIQUE INDEX [UX_TeaInventoryItems_ItemCode] ON [dbo].[TeaInventoryItems]([ItemCode]);

UPDATE [dbo].[SupplyItem]
SET [ItemCode] = CONCAT('SUPITEM-', [Id])
WHERE NULLIF(LTRIM(RTRIM([ItemCode])), '') IS NULL;
ALTER TABLE [dbo].[SupplyItem] ALTER COLUMN [ItemCode] nvarchar(50) NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_SupplyItem_ItemCode')
    CREATE UNIQUE INDEX [UX_SupplyItem_ItemCode] ON [dbo].[SupplyItem]([ItemCode]);

IF OBJECT_ID(N'[dbo].[Warehouse]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Warehouse]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouse] PRIMARY KEY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Address] nvarchar(300) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_Warehouse_IsActive] DEFAULT(1),
        [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_Warehouse_CreatedAtUtc] DEFAULT(SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX [UX_Warehouse_Code] ON [dbo].[Warehouse]([Code]);
END;

IF OBJECT_ID(N'[dbo].[WarehouseBin]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WarehouseBin]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WarehouseBin] PRIMARY KEY,
        [WarehouseId] int NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_WarehouseBin_IsActive] DEFAULT(1),
        CONSTRAINT [FK_WarehouseBin_Warehouse] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouse]([Id])
    );
    CREATE UNIQUE INDEX [UX_WarehouseBin_Warehouse_Code] ON [dbo].[WarehouseBin]([WarehouseId], [Code]);
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Warehouse] WHERE [Code] = 'MAIN')
    INSERT INTO [dbo].[Warehouse] ([Code], [Name], [Address], [IsActive])
    VALUES ('MAIN', 'Main Warehouse', NULL, 1);
DECLARE @MainWarehouseId int = (SELECT TOP(1) [Id] FROM [dbo].[Warehouse] WHERE [Code] = 'MAIN');
IF NOT EXISTS (SELECT 1 FROM [dbo].[WarehouseBin] WHERE [WarehouseId] = @MainWarehouseId AND [Code] = 'DEFAULT')
    INSERT INTO [dbo].[WarehouseBin] ([WarehouseId], [Code], [Name], [IsActive])
    VALUES (@MainWarehouseId, 'DEFAULT', 'Default Storage', 1);

IF OBJECT_ID(N'[dbo].[ProductInventoryMapping]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductInventoryMapping]
    (
        [Id] int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ProductInventoryMapping] PRIMARY KEY,
        [ProductId] int NOT NULL,
        [InventoryItemId] int NOT NULL,
        [QuantityPerUnit] decimal(18,4) NOT NULL CONSTRAINT [DF_ProductInventoryMapping_Quantity] DEFAULT(1),
        [IsActive] bit NOT NULL CONSTRAINT [DF_ProductInventoryMapping_IsActive] DEFAULT(1),
        [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ProductInventoryMapping_CreatedAtUtc] DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT [CK_ProductInventoryMapping_Quantity] CHECK ([QuantityPerUnit] > 0),
        CONSTRAINT [FK_ProductInventoryMapping_Product] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductInventoryMapping_Inventory] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[TeaInventoryItems]([Id])
    );
    CREATE UNIQUE INDEX [UX_ProductInventoryMapping_Product] ON [dbo].[ProductInventoryMapping]([ProductId]);
    CREATE INDEX [IX_ProductInventoryMapping_Inventory] ON [dbo].[ProductInventoryMapping]([InventoryItemId]);
END;

IF OBJECT_ID(N'[dbo].[OrderLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderLine]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OrderLine] PRIMARY KEY,
        [OrderId] int NOT NULL,
        [ProductId] int NULL,
        [Sku] nvarchar(50) NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_OrderLine_Discount] DEFAULT(0),
        [TaxAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_OrderLine_Tax] DEFAULT(0),
        [LineTotal] decimal(18,2) NOT NULL,
        [FulfilmentStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_OrderLine_Status] DEFAULT('Pending'),
        [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_OrderLine_CreatedAtUtc] DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT [CK_OrderLine_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_OrderLine_Order] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Order]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderLine_Product] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_OrderLine_Order] ON [dbo].[OrderLine]([OrderId]);
    CREATE INDEX [IX_OrderLine_Product] ON [dbo].[OrderLine]([ProductId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Order]') AND name = 'IX_Order_TransId')
    CREATE INDEX [IX_Order_TransId] ON [dbo].[Order]([TransId]);

IF OBJECT_ID(N'[dbo].[StockLedgerEntry]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StockLedgerEntry]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_StockLedgerEntry] PRIMARY KEY,
        [EntryNumber] uniqueidentifier NOT NULL CONSTRAINT [DF_StockLedgerEntry_Number] DEFAULT(NEWID()),
        [CorrelationId] uniqueidentifier NOT NULL,
        [WarehouseId] int NOT NULL,
        [BinId] int NULL,
        [InventoryItemId] int NULL,
        [SupplyItemId] int NULL,
        [ItemCode] nvarchar(50) NOT NULL,
        [ItemName] nvarchar(200) NOT NULL,
        [MovementType] nvarchar(40) NOT NULL,
        [QuantityChange] decimal(18,4) NOT NULL,
        [PreviousStock] decimal(18,4) NOT NULL,
        [NewStock] decimal(18,4) NOT NULL,
        [UnitCost] decimal(18,4) NULL,
        [ReferenceType] nvarchar(40) NOT NULL,
        [ReferenceId] int NULL,
        [ReferenceNumber] nvarchar(100) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [PerformedByUserId] int NULL,
        [PerformedByName] nvarchar(120) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL CONSTRAINT [DF_StockLedgerEntry_Occurred] DEFAULT(SYSUTCDATETIME()),
        [IsReversal] bit NOT NULL CONSTRAINT [DF_StockLedgerEntry_IsReversal] DEFAULT(0),
        [ReversesEntryId] bigint NULL,
        CONSTRAINT [CK_StockLedgerEntry_OneItem] CHECK
        (([InventoryItemId] IS NOT NULL AND [SupplyItemId] IS NULL) OR
         ([InventoryItemId] IS NULL AND [SupplyItemId] IS NOT NULL)),
        CONSTRAINT [CK_StockLedgerEntry_Balance] CHECK ([NewStock] >= 0 AND [NewStock] = [PreviousStock] + [QuantityChange]),
        CONSTRAINT [FK_StockLedgerEntry_Warehouse] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouse]([Id]),
        CONSTRAINT [FK_StockLedgerEntry_Bin] FOREIGN KEY ([BinId]) REFERENCES [dbo].[WarehouseBin]([Id]),
        CONSTRAINT [FK_StockLedgerEntry_Inventory] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[TeaInventoryItems]([Id]),
        CONSTRAINT [FK_StockLedgerEntry_Supply] FOREIGN KEY ([SupplyItemId]) REFERENCES [dbo].[SupplyItem]([Id]),
        CONSTRAINT [FK_StockLedgerEntry_Reversal] FOREIGN KEY ([ReversesEntryId]) REFERENCES [dbo].[StockLedgerEntry]([Id])
    );
    CREATE UNIQUE INDEX [UX_StockLedgerEntry_EntryNumber] ON [dbo].[StockLedgerEntry]([EntryNumber]);
    CREATE INDEX [IX_StockLedgerEntry_Correlation] ON [dbo].[StockLedgerEntry]([CorrelationId]);
    CREATE INDEX [IX_StockLedgerEntry_Occurred] ON [dbo].[StockLedgerEntry]([OccurredAtUtc]);
    CREATE INDEX [IX_StockLedgerEntry_Inventory_Occurred] ON [dbo].[StockLedgerEntry]([InventoryItemId], [OccurredAtUtc]);
    CREATE INDEX [IX_StockLedgerEntry_Supply_Occurred] ON [dbo].[StockLedgerEntry]([SupplyItemId], [OccurredAtUtc]);
END;

IF OBJECT_ID(N'[dbo].[InventoryImportBatch]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InventoryImportBatch]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_InventoryImportBatch] PRIMARY KEY,
        [ImportType] nvarchar(40) NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [FileSha256] nvarchar(64) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [SubmittedByUserId] int NOT NULL,
        [SubmittedByName] nvarchar(120) NOT NULL,
        [SubmittedAtUtc] datetime2 NOT NULL,
        [ApprovedByUserId] int NULL,
        [ApprovedByName] nvarchar(120) NULL,
        [ApprovedAtUtc] datetime2 NULL,
        [TotalRows] int NOT NULL,
        [ValidRows] int NOT NULL,
        [RejectedRows] int NOT NULL,
        [Notes] nvarchar(1000) NULL
    );
    CREATE INDEX [IX_InventoryImportBatch_Hash] ON [dbo].[InventoryImportBatch]([FileSha256]);
    CREATE INDEX [IX_InventoryImportBatch_Submitted] ON [dbo].[InventoryImportBatch]([SubmittedAtUtc]);
END;

IF OBJECT_ID(N'[dbo].[InventoryImportRow]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InventoryImportRow]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_InventoryImportRow] PRIMARY KEY,
        [BatchId] uniqueidentifier NOT NULL,
        [RowNumber] int NOT NULL,
        [ItemType] nvarchar(30) NOT NULL,
        [ItemCode] nvarchar(50) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [UnitCost] decimal(18,4) NULL,
        [WarehouseCode] nvarchar(30) NOT NULL,
        [BinCode] nvarchar(30) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [LedgerEntryId] bigint NULL,
        CONSTRAINT [CK_InventoryImportRow_Quantity] CHECK ([Quantity] >= 0),
        CONSTRAINT [FK_InventoryImportRow_Batch] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[InventoryImportBatch]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InventoryImportRow_Ledger] FOREIGN KEY ([LedgerEntryId]) REFERENCES [dbo].[StockLedgerEntry]([Id])
    );
    CREATE UNIQUE INDEX [UX_InventoryImportRow_Batch_Row] ON [dbo].[InventoryImportRow]([BatchId], [RowNumber]);
END;

IF OBJECT_ID(N'[dbo].[InventoryImportRowError]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InventoryImportRowError]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_InventoryImportRowError] PRIMARY KEY,
        [BatchId] uniqueidentifier NOT NULL,
        [RowNumber] int NOT NULL,
        [FieldName] nvarchar(80) NOT NULL,
        [ErrorCode] nvarchar(40) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        CONSTRAINT [FK_InventoryImportRowError_Batch] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[InventoryImportBatch]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_InventoryImportRowError_Batch_Row] ON [dbo].[InventoryImportRowError]([BatchId], [RowNumber]);
END;

IF OBJECT_ID(N'[dbo].[StockReconciliation]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StockReconciliation]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_StockReconciliation] PRIMARY KEY,
        [ReconciliationNumber] nvarchar(50) NOT NULL,
        [WarehouseId] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CountedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] int NOT NULL,
        [CreatedByName] nvarchar(120) NOT NULL,
        [ApprovedByUserId] int NULL,
        [ApprovedByName] nvarchar(120) NULL,
        [ApprovedAtUtc] datetime2 NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [FK_StockReconciliation_Warehouse] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouse]([Id])
    );
    CREATE UNIQUE INDEX [UX_StockReconciliation_Number] ON [dbo].[StockReconciliation]([ReconciliationNumber]);
END;

IF OBJECT_ID(N'[dbo].[StockReconciliationLine]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StockReconciliationLine]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_StockReconciliationLine] PRIMARY KEY,
        [ReconciliationId] uniqueidentifier NOT NULL,
        [InventoryItemId] int NULL,
        [SupplyItemId] int NULL,
        [ItemCode] nvarchar(50) NOT NULL,
        [ItemName] nvarchar(200) NOT NULL,
        [SystemQuantity] decimal(18,4) NOT NULL,
        [CountedQuantity] decimal(18,4) NOT NULL,
        [Difference] decimal(18,4) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [LedgerEntryId] bigint NULL,
        CONSTRAINT [CK_StockReconciliationLine_OneItem] CHECK
        (([InventoryItemId] IS NOT NULL AND [SupplyItemId] IS NULL) OR
         ([InventoryItemId] IS NULL AND [SupplyItemId] IS NOT NULL)),
        CONSTRAINT [FK_StockReconciliationLine_Header] FOREIGN KEY ([ReconciliationId]) REFERENCES [dbo].[StockReconciliation]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockReconciliationLine_Ledger] FOREIGN KEY ([LedgerEntryId]) REFERENCES [dbo].[StockLedgerEntry]([Id])
    );
    CREATE INDEX [IX_StockReconciliationLine_Header] ON [dbo].[StockReconciliationLine]([ReconciliationId]);
END;

IF OBJECT_ID(N'[dbo].[QRCodeScan]', N'U') IS NOT NULL
BEGIN
    DECLARE @QrFk sysname;
    SELECT TOP(1) @QrFk = fk.name
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'[dbo].[QRCodeScan]');
    IF @QrFk IS NOT NULL EXEC(N'ALTER TABLE [dbo].[QRCodeScan] DROP CONSTRAINT [' + @QrFk + N']');

    IF COL_LENGTH('dbo.QRCodeScan', 'ScannedByName') IS NULL ALTER TABLE [dbo].[QRCodeScan] ADD [ScannedByName] nvarchar(120) NOT NULL CONSTRAINT [DF_QRCodeScan_ScannedByName] DEFAULT('');
    IF COL_LENGTH('dbo.QRCodeScan', 'EntityType') IS NULL ALTER TABLE [dbo].[QRCodeScan] ADD [EntityType] nvarchar(40) NOT NULL CONSTRAINT [DF_QRCodeScan_EntityType] DEFAULT('');
    IF COL_LENGTH('dbo.QRCodeScan', 'EntityId') IS NULL ALTER TABLE [dbo].[QRCodeScan] ADD [EntityId] int NULL;
    IF COL_LENGTH('dbo.QRCodeScan', 'WasSuccessful') IS NULL ALTER TABLE [dbo].[QRCodeScan] ADD [WasSuccessful] bit NOT NULL CONSTRAINT [DF_QRCodeScan_WasSuccessful] DEFAULT(0);
    IF COL_LENGTH('dbo.QRCodeScan', 'IpAddress') IS NULL ALTER TABLE [dbo].[QRCodeScan] ADD [IpAddress] nvarchar(64) NOT NULL CONSTRAINT [DF_QRCodeScan_IpAddress] DEFAULT('');
    IF COL_LENGTH('dbo.QRCodeScan', 'UserAgent') IS NULL ALTER TABLE [dbo].[QRCodeScan] ADD [UserAgent] nvarchar(500) NOT NULL CONSTRAINT [DF_QRCodeScan_UserAgent] DEFAULT('');
    IF COL_LENGTH('dbo.QRCodeScan', 'CorrelationId') IS NULL ALTER TABLE [dbo].[QRCodeScan] ADD [CorrelationId] uniqueidentifier NOT NULL CONSTRAINT [DF_QRCodeScan_CorrelationId] DEFAULT(NEWID());
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QRCodeScan]') AND name = 'IX_QRCodeScan_Date') CREATE INDEX [IX_QRCodeScan_Date] ON [dbo].[QRCodeScan]([ScanDateTime]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[QRCodeScan]') AND name = 'UX_QRCodeScan_Correlation') CREATE UNIQUE INDEX [UX_QRCodeScan_Correlation] ON [dbo].[QRCodeScan]([CorrelationId]);
END;

-- Preserve the existing product quantities by converting each product into a
-- mapped finished-goods item. This is a traceable migration of current data,
-- not the creation of sample operational records.
INSERT INTO [dbo].[TeaInventoryItems]
(
    [ItemCode], [Name], [TeaType], [Grade], [Origin], [HarvestSeason], [HarvestDate],
    [BatchNumber], [Description], [CurrentStock], [Unit], [MinimumStock], [ReorderLevel],
    [ReorderQuantity], [UnitCost], [RetailPrice], [Status], [QRCodeData], [CreatedDate],
    [LastUpdated], [HasBeenCorrected], [LastCorrectionDate], [LastCorrectedBy], [CorrectionReason]
)
SELECT
    CONCAT('PROD-', p.[Id]), COALESCE(NULLIF(p.[Title], ''), CONCAT('Product ', p.[Id])),
    'Retail Product', 'N/A', '', '', NULL, '', 'Migrated from existing product stock',
    CONVERT(decimal(10,2), ISNULL(p.[Quantity], 0)), 'Each', 0, 0, 0, NULL, p.[Price],
    'Active', CONCAT('PROD-', p.[Id]), GETDATE(), GETDATE(), 0, NULL, '', ''
FROM [dbo].[Products] p
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ProductInventoryMapping] m WHERE m.[ProductId] = p.[Id])
  AND NOT EXISTS (SELECT 1 FROM [dbo].[TeaInventoryItems] t WHERE t.[ItemCode] = CONCAT('PROD-', p.[Id]));

INSERT INTO [dbo].[ProductInventoryMapping]
    ([ProductId], [InventoryItemId], [QuantityPerUnit], [IsActive], [CreatedAtUtc])
SELECT p.[Id], t.[Id], 1, 1, SYSUTCDATETIME()
FROM [dbo].[Products] p
JOIN [dbo].[TeaInventoryItems] t ON t.[ItemCode] = CONCAT('PROD-', p.[Id])
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ProductInventoryMapping] m WHERE m.[ProductId] = p.[Id]);

DECLARE @DefaultBinId int = (SELECT TOP(1) [Id] FROM [dbo].[WarehouseBin] WHERE [WarehouseId] = @MainWarehouseId AND [Code] = 'DEFAULT');
INSERT INTO [dbo].[StockLedgerEntry]
(
    [EntryNumber], [CorrelationId], [WarehouseId], [BinId], [InventoryItemId], [SupplyItemId],
    [ItemCode], [ItemName], [MovementType], [QuantityChange], [PreviousStock], [NewStock],
    [UnitCost], [ReferenceType], [ReferenceId], [ReferenceNumber], [Reason],
    [PerformedByUserId], [PerformedByName], [OccurredAtUtc], [IsReversal], [ReversesEntryId]
)
SELECT NEWID(), NEWID(), @MainWarehouseId, @DefaultBinId, t.[Id], NULL,
       t.[ItemCode], t.[Name], 'OpeningBalance', t.[CurrentStock], 0, t.[CurrentStock],
       t.[UnitCost], 'SystemMigration', NULL, 'INDUSTRIAL-MIGRATION-20260715',
       'Opening balance migrated from the existing recorded stock quantity', NULL,
       'System Migration', SYSUTCDATETIME(), 0, NULL
FROM [dbo].[TeaInventoryItems] t
WHERE t.[CurrentStock] > 0
  AND NOT EXISTS (SELECT 1 FROM [dbo].[StockLedgerEntry] l WHERE l.[InventoryItemId] = t.[Id]);

INSERT INTO [dbo].[StockLedgerEntry]
(
    [EntryNumber], [CorrelationId], [WarehouseId], [BinId], [InventoryItemId], [SupplyItemId],
    [ItemCode], [ItemName], [MovementType], [QuantityChange], [PreviousStock], [NewStock],
    [UnitCost], [ReferenceType], [ReferenceId], [ReferenceNumber], [Reason],
    [PerformedByUserId], [PerformedByName], [OccurredAtUtc], [IsReversal], [ReversesEntryId]
)
SELECT NEWID(), NEWID(), @MainWarehouseId, @DefaultBinId, NULL, s.[Id],
       s.[ItemCode], s.[Name], 'OpeningBalance', s.[CurrentStock], 0, s.[CurrentStock],
       NULL, 'SystemMigration', NULL, 'INDUSTRIAL-MIGRATION-20260715',
       'Opening balance migrated from the existing recorded stock quantity', NULL,
       'System Migration', SYSUTCDATETIME(), 0, NULL
FROM [dbo].[SupplyItem] s
WHERE s.[CurrentStock] > 0
  AND NOT EXISTS (SELECT 1 FROM [dbo].[StockLedgerEntry] l WHERE l.[SupplyItemId] = s.[Id]);

IF OBJECT_ID(N'[dbo].[TR_StockLedgerEntry_Immutable]', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER [dbo].[TR_StockLedgerEntry_Immutable]
          ON [dbo].[StockLedgerEntry]
          INSTEAD OF UPDATE, DELETE
          AS
          BEGIN
              SET NOCOUNT ON;
              THROW 51000, ''Stock ledger entries are immutable. Create a reversal entry instead.'', 1;
          END');
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[TR_StockLedgerEntry_Immutable]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_StockLedgerEntry_Immutable];
IF OBJECT_ID(N'[dbo].[StockReconciliationLine]', N'U') IS NOT NULL DROP TABLE [dbo].[StockReconciliationLine];
IF OBJECT_ID(N'[dbo].[StockReconciliation]', N'U') IS NOT NULL DROP TABLE [dbo].[StockReconciliation];
IF OBJECT_ID(N'[dbo].[InventoryImportRowError]', N'U') IS NOT NULL DROP TABLE [dbo].[InventoryImportRowError];
IF OBJECT_ID(N'[dbo].[InventoryImportRow]', N'U') IS NOT NULL DROP TABLE [dbo].[InventoryImportRow];
IF OBJECT_ID(N'[dbo].[InventoryImportBatch]', N'U') IS NOT NULL DROP TABLE [dbo].[InventoryImportBatch];
IF OBJECT_ID(N'[dbo].[StockLedgerEntry]', N'U') IS NOT NULL DROP TABLE [dbo].[StockLedgerEntry];
IF OBJECT_ID(N'[dbo].[OrderLine]', N'U') IS NOT NULL DROP TABLE [dbo].[OrderLine];
IF OBJECT_ID(N'[dbo].[ProductInventoryMapping]', N'U') IS NOT NULL DROP TABLE [dbo].[ProductInventoryMapping];
IF OBJECT_ID(N'[dbo].[WarehouseBin]', N'U') IS NOT NULL DROP TABLE [dbo].[WarehouseBin];
IF OBJECT_ID(N'[dbo].[Warehouse]', N'U') IS NOT NULL DROP TABLE [dbo].[Warehouse];
""");
        }
    }
}
