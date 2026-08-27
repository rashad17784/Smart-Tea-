-- Create TeaInventoryItems table
CREATE TABLE [dbo].[TeaInventoryItems] (
    [Id]                INT                 IDENTITY (1, 1) NOT NULL,
    [Name]              NVARCHAR (100)      NOT NULL,
    [TeaType]           NVARCHAR (50)       NOT NULL,
    [Grade]             NVARCHAR (50)       NOT NULL,
    [Origin]            NVARCHAR (100)      NULL,
    [HarvestSeason]     NVARCHAR (50)       NULL,
    [HarvestDate]       DATE                NULL,
    [BatchNumber]       NVARCHAR (50)       NULL,
    [Description]       NVARCHAR (MAX)      NULL,
    [CurrentStock]      DECIMAL (18, 2)     NOT NULL,
    [Unit]              NVARCHAR (10)       NOT NULL,
    [MinimumStock]      DECIMAL (18, 2)     NULL,
    [ReorderLevel]      DECIMAL (18, 2)     NULL,
    [ReorderQuantity]   DECIMAL (18, 2)     NULL,
    [UnitCost]          DECIMAL (18, 2)     NULL,
    [RetailPrice]       DECIMAL (18, 2)     NULL,
    [Status]            NVARCHAR (20)       NOT NULL,
    [QRCodeData]        NVARCHAR (100)      NOT NULL,
    [CreatedDate]       DATETIME            NOT NULL,
    [LastUpdated]       DATETIME            NULL,
    [HasBeenCorrected]  BIT                 DEFAULT ((0)) NOT NULL,
    [LastCorrectionDate] DATETIME           NULL,
    [LastCorrectedBy]   NVARCHAR (100)      NULL,
    [CorrectionReason]  NVARCHAR (MAX)      NULL,
    CONSTRAINT [PK_TeaInventoryItems] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UK_TeaInventoryItems_QRCodeData] UNIQUE NONCLUSTERED ([QRCodeData] ASC)
);

-- Create TeaInventoryTransactions table
CREATE TABLE [dbo].[TeaInventoryTransactions] (
    [Id]                  INT                 IDENTITY (1, 1) NOT NULL,
    [InventoryItemId]     INT                 NOT NULL,
    [TransactionDate]     DATETIME            NOT NULL,
    [TransactionType]     NVARCHAR (50)       NOT NULL,
    [Quantity]            DECIMAL (18, 2)     NOT NULL,
    [PreviousStock]       DECIMAL (18, 2)     NOT NULL,
    [NewStock]            DECIMAL (18, 2)     NOT NULL,
    [ReferenceNumber]     NVARCHAR (50)       NULL,
    [Notes]               NVARCHAR (MAX)      NULL,
    [PerformedBy]         NVARCHAR (100)      NULL,
    [IsCorrection]        BIT                 DEFAULT ((0)) NOT NULL,
    [UnitPrice]           DECIMAL (18, 2)     NULL,
    [ReferenceId]         INT                 NULL,
    [CorrectionReason]    NVARCHAR (MAX)      NULL,
    [QRCodeScanned]       NVARCHAR (100)      NULL,
    [RelatedTransactionId] INT                NULL,
    CONSTRAINT [PK_TeaInventoryTransactions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TeaInventoryTransactions_TeaInventoryItems] FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[TeaInventoryItems] ([Id]) ON DELETE CASCADE
);

-- Create indexes for better query performance
CREATE INDEX [IX_TeaInventoryItems_TeaType] ON [dbo].[TeaInventoryItems] ([TeaType]);
CREATE INDEX [IX_TeaInventoryItems_Status] ON [dbo].[TeaInventoryItems] ([Status]);
CREATE INDEX [IX_TeaInventoryItems_BatchNumber] ON [dbo].[TeaInventoryItems] ([BatchNumber]);

CREATE INDEX [IX_TeaInventoryTransactions_InventoryItemId] ON [dbo].[TeaInventoryTransactions] ([InventoryItemId]);
CREATE INDEX [IX_TeaInventoryTransactions_TransactionDate] ON [dbo].[TeaInventoryTransactions] ([TransactionDate]);
CREATE INDEX [IX_TeaInventoryTransactions_TransactionType] ON [dbo].[TeaInventoryTransactions] ([TransactionType]); 