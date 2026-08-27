using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaOnlineShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseStockBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[StockBalance]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StockBalance]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_StockBalance] PRIMARY KEY,
        [WarehouseId] int NOT NULL,
        [BinId] int NOT NULL,
        [InventoryItemId] int NULL,
        [SupplyItemId] int NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [LastUpdatedUtc] datetime2 NOT NULL CONSTRAINT [DF_StockBalance_Updated] DEFAULT(SYSUTCDATETIME()),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [CK_StockBalance_OneItem] CHECK
        (([InventoryItemId] IS NOT NULL AND [SupplyItemId] IS NULL) OR
         ([InventoryItemId] IS NULL AND [SupplyItemId] IS NOT NULL)),
        CONSTRAINT [CK_StockBalance_NonNegative] CHECK ([Quantity] >= 0),
        CONSTRAINT [FK_StockBalance_Warehouse] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouse]([Id]),
        CONSTRAINT [FK_StockBalance_Bin] FOREIGN KEY ([BinId]) REFERENCES [dbo].[WarehouseBin]([Id]),
        CONSTRAINT [FK_StockBalance_Inventory] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[TeaInventoryItems]([Id]),
        CONSTRAINT [FK_StockBalance_Supply] FOREIGN KEY ([SupplyItemId]) REFERENCES [dbo].[SupplyItem]([Id])
    );
    CREATE UNIQUE INDEX [UX_StockBalance_Inventory]
        ON [dbo].[StockBalance]([WarehouseId], [BinId], [InventoryItemId])
        WHERE [InventoryItemId] IS NOT NULL;
    CREATE UNIQUE INDEX [UX_StockBalance_Supply]
        ON [dbo].[StockBalance]([WarehouseId], [BinId], [SupplyItemId])
        WHERE [SupplyItemId] IS NOT NULL;
END;

DECLARE @WarehouseId int = (SELECT TOP(1) [Id] FROM [dbo].[Warehouse] WHERE [Code] = 'MAIN');
DECLARE @BinId int = (SELECT TOP(1) [Id] FROM [dbo].[WarehouseBin] WHERE [WarehouseId] = @WarehouseId AND [Code] = 'DEFAULT');

INSERT INTO [dbo].[StockBalance]
    ([WarehouseId], [BinId], [InventoryItemId], [SupplyItemId], [Quantity], [LastUpdatedUtc])
SELECT @WarehouseId, @BinId, t.[Id], NULL, t.[CurrentStock], SYSUTCDATETIME()
FROM [dbo].[TeaInventoryItems] t
WHERE NOT EXISTS
(
    SELECT 1 FROM [dbo].[StockBalance] b
    WHERE b.[WarehouseId] = @WarehouseId AND b.[BinId] = @BinId AND b.[InventoryItemId] = t.[Id]
);

INSERT INTO [dbo].[StockBalance]
    ([WarehouseId], [BinId], [InventoryItemId], [SupplyItemId], [Quantity], [LastUpdatedUtc])
SELECT @WarehouseId, @BinId, NULL, s.[Id], s.[CurrentStock], SYSUTCDATETIME()
FROM [dbo].[SupplyItem] s
WHERE NOT EXISTS
(
    SELECT 1 FROM [dbo].[StockBalance] b
    WHERE b.[WarehouseId] = @WarehouseId AND b.[BinId] = @BinId AND b.[SupplyItemId] = s.[Id]
);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[StockBalance]', N'U') IS NOT NULL DROP TABLE [dbo].[StockBalance];
""");
        }
    }
}
