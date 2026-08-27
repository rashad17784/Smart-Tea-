using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TeaOnlineShop.Identity.Migrations;

[DbContext(typeof(ApplicationIdentityContext))]
[Migration("20260715103000_AddOrderPaymentAudit")]
public sealed class AddOrderPaymentAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF COL_LENGTH('dbo.[Order]', 'PaymentMethod') IS NULL
    ALTER TABLE [dbo].[Order] ADD [PaymentMethod] nvarchar(40) NOT NULL CONSTRAINT [DF_Order_PaymentMethod] DEFAULT(N'CashOnDelivery');
IF COL_LENGTH('dbo.[Order]', 'PaymentStatus') IS NULL
    ALTER TABLE [dbo].[Order] ADD [PaymentStatus] nvarchar(40) NOT NULL CONSTRAINT [DF_Order_PaymentStatus] DEFAULT(N'PendingCollection');

IF OBJECT_ID(N'[dbo].[OrderPaymentEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderPaymentEvent]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OrderPaymentEvent] PRIMARY KEY,
        [OrderId] int NOT NULL,
        [FromStatus] nvarchar(40) NOT NULL,
        [ToStatus] nvarchar(40) NOT NULL,
        [Method] nvarchar(40) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reference] nvarchar(120) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [RecordedByUserId] int NULL,
        [RecordedByName] nvarchar(120) NOT NULL,
        [RecordedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_OrderPaymentEvent_RecordedAtUtc] DEFAULT(SYSUTCDATETIME()),
        CONSTRAINT [FK_OrderPaymentEvent_Order] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Order]([Id])
    );
    CREATE INDEX [IX_OrderPaymentEvent_Order_Recorded] ON [dbo].[OrderPaymentEvent]([OrderId], [RecordedAtUtc]);
END;

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OrderPaymentEvent_Immutable]
ON [dbo].[OrderPaymentEvent]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51002, ''Order payment history is append-only and cannot be modified or deleted.'', 1;
END');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[TR_OrderPaymentEvent_Immutable]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OrderPaymentEvent_Immutable];
IF OBJECT_ID(N'[dbo].[OrderPaymentEvent]', N'U') IS NOT NULL DROP TABLE [dbo].[OrderPaymentEvent];
IF COL_LENGTH('dbo.[Order]', 'PaymentStatus') IS NOT NULL ALTER TABLE [dbo].[Order] DROP CONSTRAINT [DF_Order_PaymentStatus];
IF COL_LENGTH('dbo.[Order]', 'PaymentStatus') IS NOT NULL ALTER TABLE [dbo].[Order] DROP COLUMN [PaymentStatus];
IF COL_LENGTH('dbo.[Order]', 'PaymentMethod') IS NOT NULL ALTER TABLE [dbo].[Order] DROP CONSTRAINT [DF_Order_PaymentMethod];
IF COL_LENGTH('dbo.[Order]', 'PaymentMethod') IS NOT NULL ALTER TABLE [dbo].[Order] DROP COLUMN [PaymentMethod];
""");
    }
}
