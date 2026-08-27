using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TeaOnlineShop.Identity.Migrations;

[DbContext(typeof(ApplicationIdentityContext))]
[Migration("20260715090000_AddOrderFulfillmentAudit")]
public sealed class AddOrderFulfillmentAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF COL_LENGTH('dbo.[Order]', 'Carrier') IS NULL
    ALTER TABLE [dbo].[Order] ADD [Carrier] nvarchar(100) NULL;
IF COL_LENGTH('dbo.[Order]', 'TrackingNumber') IS NULL
    ALTER TABLE [dbo].[Order] ADD [TrackingNumber] nvarchar(100) NULL;
IF COL_LENGTH('dbo.[Order]', 'ShippedAtUtc') IS NULL
    ALTER TABLE [dbo].[Order] ADD [ShippedAtUtc] datetime2 NULL;
IF COL_LENGTH('dbo.[Order]', 'ShippedByUserId') IS NULL
    ALTER TABLE [dbo].[Order] ADD [ShippedByUserId] int NULL;
IF COL_LENGTH('dbo.[Order]', 'ShippedByName') IS NULL
    ALTER TABLE [dbo].[Order] ADD [ShippedByName] nvarchar(120) NULL;

IF OBJECT_ID(N'[dbo].[OrderStatusHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderStatusHistory]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OrderStatusHistory] PRIMARY KEY,
        [OrderId] int NOT NULL,
        [FromStatus] nvarchar(50) NOT NULL,
        [ToStatus] nvarchar(50) NOT NULL,
        [ChangedByUserId] int NULL,
        [ChangedByName] nvarchar(120) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [ChangedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_OrderStatusHistory_ChangedAtUtc] DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT [FK_OrderStatusHistory_Order] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Order]([Id])
    );
    CREATE INDEX [IX_OrderStatusHistory_Order_Changed] ON [dbo].[OrderStatusHistory]([OrderId], [ChangedAtUtc]);
END;

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OrderStatusHistory_Immutable]
ON [dbo].[OrderStatusHistory]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51001, ''Order status history is append-only and cannot be modified or deleted.'', 1;
END');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[TR_OrderStatusHistory_Immutable]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OrderStatusHistory_Immutable];
IF OBJECT_ID(N'[dbo].[OrderStatusHistory]', N'U') IS NOT NULL DROP TABLE [dbo].[OrderStatusHistory];
IF COL_LENGTH('dbo.[Order]', 'ShippedByName') IS NOT NULL ALTER TABLE [dbo].[Order] DROP COLUMN [ShippedByName];
IF COL_LENGTH('dbo.[Order]', 'ShippedByUserId') IS NOT NULL ALTER TABLE [dbo].[Order] DROP COLUMN [ShippedByUserId];
IF COL_LENGTH('dbo.[Order]', 'ShippedAtUtc') IS NOT NULL ALTER TABLE [dbo].[Order] DROP COLUMN [ShippedAtUtc];
IF COL_LENGTH('dbo.[Order]', 'TrackingNumber') IS NOT NULL ALTER TABLE [dbo].[Order] DROP COLUMN [TrackingNumber];
IF COL_LENGTH('dbo.[Order]', 'Carrier') IS NOT NULL ALTER TABLE [dbo].[Order] DROP COLUMN [Carrier];
""");
    }
}
