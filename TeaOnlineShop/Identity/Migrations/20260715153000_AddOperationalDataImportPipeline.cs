using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TeaOnlineShop.Identity.Migrations;

[DbContext(typeof(ApplicationIdentityContext))]
[Migration("20260715153000_AddOperationalDataImportPipeline")]
public sealed class AddOperationalDataImportPipeline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;

IF COL_LENGTH('dbo.AiPredictionHistory', 'DataSource') IS NOT NULL
    ALTER TABLE [dbo].[AiPredictionHistory] ALTER COLUMN [DataSource] nvarchar(120) NOT NULL;

IF OBJECT_ID(N'[dbo].[OperationalDataImportBatch]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OperationalDataImportBatch]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_OperationalDataImportBatch] PRIMARY KEY,
        [BatchNumber] nvarchar(40) NOT NULL,
        [SourceSystem] nvarchar(80) NOT NULL,
        [SourceDocumentReference] nvarchar(120) NOT NULL,
        [SourcePeriodStartUtc] datetime2 NOT NULL,
        [SourcePeriodEndUtc] datetime2 NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSha256] nvarchar(64) NOT NULL,
        [OriginalFile] varbinary(max) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [SubmittedByUserId] int NOT NULL,
        [SubmittedByName] nvarchar(120) NOT NULL,
        [SubmittedAtUtc] datetime2 NOT NULL,
        [ApprovedByUserId] int NULL,
        [ApprovedByName] nvarchar(120) NULL,
        [ApprovedAtUtc] datetime2 NULL,
        [RejectedByUserId] int NULL,
        [RejectedByName] nvarchar(120) NULL,
        [RejectedAtUtc] datetime2 NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [SourceAuthenticityCertified] bit NOT NULL,
        [ExpectedRowCount] int NOT NULL,
        [ExpectedInboundKg] decimal(18,4) NOT NULL,
        [ExpectedOutboundKg] decimal(18,4) NOT NULL,
        [ParsedRowCount] int NOT NULL,
        [ValidRowCount] int NOT NULL,
        [RejectedRowCount] int NOT NULL,
        [DuplicateRowCount] int NOT NULL,
        [CalculatedInboundKg] decimal(18,4) NOT NULL,
        [CalculatedOutboundKg] decimal(18,4) NOT NULL,
        [ReconciliationStatus] nvarchar(30) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [CK_OperationalBatch_SourcePeriod] CHECK ([SourcePeriodEndUtc] >= [SourcePeriodStartUtc]),
        CONSTRAINT [CK_OperationalBatch_Totals] CHECK ([ExpectedRowCount] >= 0 AND [ExpectedInboundKg] >= 0 AND [ExpectedOutboundKg] >= 0)
    );
    CREATE UNIQUE INDEX [UX_OperationalBatch_Number] ON [dbo].[OperationalDataImportBatch]([BatchNumber]);
    CREATE UNIQUE INDEX [UX_OperationalBatch_FileHash] ON [dbo].[OperationalDataImportBatch]([FileSha256]);
    CREATE INDEX [IX_OperationalBatch_SourcePeriod] ON [dbo].[OperationalDataImportBatch]([SourceSystem], [SourcePeriodStartUtc], [SourcePeriodEndUtc]);
    CREATE INDEX [IX_OperationalBatch_StatusSubmitted] ON [dbo].[OperationalDataImportBatch]([Status], [SubmittedAtUtc]);
END;

IF OBJECT_ID(N'[dbo].[OperationalDataImportRow]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OperationalDataImportRow]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OperationalDataImportRow] PRIMARY KEY,
        [BatchId] uniqueidentifier NOT NULL,
        [RowNumber] int NOT NULL,
        [SourceSystem] nvarchar(80) NOT NULL,
        [SourceRecordId] nvarchar(120) NOT NULL,
        [OriginalTransactionAtUtc] datetime2 NOT NULL,
        [TeaGrade] nvarchar(20) NOT NULL,
        [ItemCode] nvarchar(50) NOT NULL,
        [InventoryItemId] int NULL,
        [QuantityKg] decimal(18,4) NOT NULL,
        [OriginalUnit] nvarchar(20) NOT NULL,
        [TransactionType] nvarchar(40) NOT NULL,
        [QuantityChangeKg] decimal(18,4) NOT NULL,
        [IsDemand] bit NOT NULL,
        [SourceReferenceNumber] nvarchar(120) NOT NULL,
        [SupplierOrProductionReference] nvarchar(120) NOT NULL,
        [WarehouseCode] nvarchar(30) NOT NULL,
        [BinCode] nvarchar(30) NOT NULL,
        [UnitCost] decimal(18,4) NULL,
        [Reason] nvarchar(500) NOT NULL,
        [CanonicalSha256] nvarchar(64) NOT NULL,
        [RawData] nvarchar(max) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        CONSTRAINT [FK_OperationalRow_Batch] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[OperationalDataImportBatch]([Id]),
        CONSTRAINT [FK_OperationalRow_Inventory] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[TeaInventoryItems]([Id]),
        CONSTRAINT [CK_OperationalRow_Quantity] CHECK ([QuantityKg] >= 0)
    );
    CREATE UNIQUE INDEX [UX_OperationalRow_BatchRow] ON [dbo].[OperationalDataImportRow]([BatchId], [RowNumber]);
    CREATE INDEX [IX_OperationalRow_SourceId] ON [dbo].[OperationalDataImportRow]([SourceSystem], [SourceRecordId]);
    CREATE INDEX [IX_OperationalRow_Hash] ON [dbo].[OperationalDataImportRow]([CanonicalSha256]);
END;

IF OBJECT_ID(N'[dbo].[OperationalDataImportRowError]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OperationalDataImportRowError]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OperationalDataImportRowError] PRIMARY KEY,
        [BatchId] uniqueidentifier NOT NULL,
        [RowNumber] int NOT NULL,
        [FieldName] nvarchar(80) NOT NULL,
        [ErrorCode] nvarchar(50) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        CONSTRAINT [FK_OperationalError_Batch] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[OperationalDataImportBatch]([Id])
    );
    CREATE INDEX [IX_OperationalError_BatchRow] ON [dbo].[OperationalDataImportRowError]([BatchId], [RowNumber]);
END;

IF OBJECT_ID(N'[dbo].[OperationalDataImportAuditEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OperationalDataImportAuditEvent]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OperationalDataImportAuditEvent] PRIMARY KEY,
        [BatchId] uniqueidentifier NOT NULL,
        [Action] nvarchar(60) NOT NULL,
        [FromStatus] nvarchar(30) NOT NULL,
        [ToStatus] nvarchar(30) NOT NULL,
        [ActorUserId] int NULL,
        [ActorName] nvarchar(120) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        CONSTRAINT [FK_OperationalAudit_Batch] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[OperationalDataImportBatch]([Id])
    );
    CREATE INDEX [IX_OperationalAudit_BatchTime] ON [dbo].[OperationalDataImportAuditEvent]([BatchId], [OccurredAtUtc]);
END;

IF OBJECT_ID(N'[dbo].[OperationalInventoryEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OperationalInventoryEvent]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OperationalInventoryEvent] PRIMARY KEY,
        [PublicId] uniqueidentifier NOT NULL,
        [BatchId] uniqueidentifier NOT NULL,
        [ImportRowId] bigint NOT NULL,
        [SourceSystem] nvarchar(80) NOT NULL,
        [SourceRecordId] nvarchar(120) NOT NULL,
        [SourceOccurredAtUtc] datetime2 NOT NULL,
        [ImportedAtUtc] datetime2 NOT NULL,
        [TeaGrade] nvarchar(20) NOT NULL,
        [ItemCode] nvarchar(50) NOT NULL,
        [InventoryItemId] int NOT NULL,
        [QuantityKg] decimal(18,4) NOT NULL,
        [QuantityChangeKg] decimal(18,4) NOT NULL,
        [TransactionType] nvarchar(40) NOT NULL,
        [IsDemand] bit NOT NULL,
        [SourceReferenceNumber] nvarchar(120) NOT NULL,
        [SupplierOrProductionReference] nvarchar(120) NOT NULL,
        [WarehouseCode] nvarchar(30) NOT NULL,
        [BinCode] nvarchar(30) NOT NULL,
        [UnitCost] decimal(18,4) NULL,
        [Reason] nvarchar(500) NOT NULL,
        [CanonicalSha256] nvarchar(64) NOT NULL,
        [ImportedByUserId] int NOT NULL,
        [ImportedByName] nvarchar(120) NOT NULL,
        CONSTRAINT [FK_OperationalEvent_Batch] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[OperationalDataImportBatch]([Id]),
        CONSTRAINT [FK_OperationalEvent_Row] FOREIGN KEY ([ImportRowId]) REFERENCES [dbo].[OperationalDataImportRow]([Id]),
        CONSTRAINT [FK_OperationalEvent_Inventory] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[TeaInventoryItems]([Id]),
        CONSTRAINT [CK_OperationalEvent_Quantity] CHECK ([QuantityKg] > 0 AND [QuantityChangeKg] <> 0)
    );
    CREATE UNIQUE INDEX [UX_OperationalEvent_PublicId] ON [dbo].[OperationalInventoryEvent]([PublicId]);
    CREATE UNIQUE INDEX [UX_OperationalEvent_SourceId] ON [dbo].[OperationalInventoryEvent]([SourceSystem], [SourceRecordId]);
    CREATE UNIQUE INDEX [UX_OperationalEvent_ImportRow] ON [dbo].[OperationalInventoryEvent]([ImportRowId]);
    CREATE INDEX [IX_OperationalEvent_GradeDemandDate] ON [dbo].[OperationalInventoryEvent]([TeaGrade], [IsDemand], [SourceOccurredAtUtc]);
    CREATE INDEX [IX_OperationalEvent_BatchDate] ON [dbo].[OperationalInventoryEvent]([BatchId], [SourceOccurredAtUtc]);
END;

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OperationalEvent_Immutable]
ON [dbo].[OperationalInventoryEvent]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51010, ''Published operational events are immutable.'', 1;
END');

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OperationalAudit_AppendOnly]
ON [dbo].[OperationalDataImportAuditEvent]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51011, ''Operational import audit events are append-only.'', 1;
END');

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OperationalErrors_AppendOnly]
ON [dbo].[OperationalDataImportRowError]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51012, ''Operational import validation findings cannot be modified or deleted.'', 1;
END');

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OperationalRows_Protected]
ON [dbo].[OperationalDataImportRow]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE([BatchId]) OR UPDATE([RowNumber]) OR UPDATE([SourceSystem]) OR UPDATE([SourceRecordId])
       OR UPDATE([OriginalTransactionAtUtc]) OR UPDATE([TeaGrade]) OR UPDATE([ItemCode]) OR UPDATE([InventoryItemId])
       OR UPDATE([QuantityKg]) OR UPDATE([OriginalUnit]) OR UPDATE([TransactionType]) OR UPDATE([QuantityChangeKg])
       OR UPDATE([IsDemand]) OR UPDATE([SourceReferenceNumber]) OR UPDATE([SupplierOrProductionReference])
       OR UPDATE([WarehouseCode]) OR UPDATE([BinCode]) OR UPDATE([UnitCost]) OR UPDATE([Reason])
       OR UPDATE([CanonicalSha256]) OR UPDATE([RawData])
    BEGIN
        THROW 51013, ''Staged operational source evidence cannot be changed.'', 1;
    END;
    IF EXISTS (SELECT 1 FROM deleted WHERE [Status] = N''Published'')
        THROW 51014, ''Published staging rows are final.'', 1;
END');

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OperationalRows_NoDelete]
ON [dbo].[OperationalDataImportRow]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51015, ''Operational staging rows are retained as audit evidence.'', 1;
END');

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OperationalBatch_Protected]
ON [dbo].[OperationalDataImportBatch]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE([BatchNumber]) OR UPDATE([SourceSystem]) OR UPDATE([SourceDocumentReference])
       OR UPDATE([SourcePeriodStartUtc]) OR UPDATE([SourcePeriodEndUtc]) OR UPDATE([FileName])
       OR UPDATE([ContentType]) OR UPDATE([FileSha256]) OR UPDATE([OriginalFile])
       OR UPDATE([SubmittedByUserId]) OR UPDATE([SubmittedByName]) OR UPDATE([SubmittedAtUtc])
       OR UPDATE([SourceAuthenticityCertified]) OR UPDATE([ExpectedRowCount])
       OR UPDATE([ExpectedInboundKg]) OR UPDATE([ExpectedOutboundKg]) OR UPDATE([ParsedRowCount])
       OR UPDATE([ValidRowCount]) OR UPDATE([RejectedRowCount]) OR UPDATE([DuplicateRowCount])
       OR UPDATE([CalculatedInboundKg]) OR UPDATE([CalculatedOutboundKg]) OR UPDATE([ReconciliationStatus])
    BEGIN
        THROW 51016, ''The certified operational import manifest and evidence cannot be changed.'', 1;
    END;
    IF EXISTS (SELECT 1 FROM deleted WHERE [Status] IN (N''Approved'', N''Rejected''))
        THROW 51017, ''Final operational import batches cannot be changed.'', 1;
END');

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_OperationalBatch_NoDelete]
ON [dbo].[OperationalDataImportBatch]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51018, ''Operational import batches are retained as audit evidence.'', 1;
END');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[TR_OperationalBatch_NoDelete]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OperationalBatch_NoDelete];
IF OBJECT_ID(N'[dbo].[TR_OperationalBatch_Protected]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OperationalBatch_Protected];
IF OBJECT_ID(N'[dbo].[TR_OperationalRows_NoDelete]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OperationalRows_NoDelete];
IF OBJECT_ID(N'[dbo].[TR_OperationalRows_Protected]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OperationalRows_Protected];
IF OBJECT_ID(N'[dbo].[TR_OperationalErrors_AppendOnly]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OperationalErrors_AppendOnly];
IF OBJECT_ID(N'[dbo].[TR_OperationalAudit_AppendOnly]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OperationalAudit_AppendOnly];
IF OBJECT_ID(N'[dbo].[TR_OperationalEvent_Immutable]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_OperationalEvent_Immutable];
IF OBJECT_ID(N'[dbo].[OperationalInventoryEvent]', N'U') IS NOT NULL DROP TABLE [dbo].[OperationalInventoryEvent];
IF OBJECT_ID(N'[dbo].[OperationalDataImportAuditEvent]', N'U') IS NOT NULL DROP TABLE [dbo].[OperationalDataImportAuditEvent];
IF OBJECT_ID(N'[dbo].[OperationalDataImportRowError]', N'U') IS NOT NULL DROP TABLE [dbo].[OperationalDataImportRowError];
IF OBJECT_ID(N'[dbo].[OperationalDataImportRow]', N'U') IS NOT NULL DROP TABLE [dbo].[OperationalDataImportRow];
IF OBJECT_ID(N'[dbo].[OperationalDataImportBatch]', N'U') IS NOT NULL DROP TABLE [dbo].[OperationalDataImportBatch];
IF COL_LENGTH('dbo.AiPredictionHistory', 'DataSource') IS NOT NULL
    ALTER TABLE [dbo].[AiPredictionHistory] ALTER COLUMN [DataSource] nvarchar(50) NOT NULL;
""");
    }
}
