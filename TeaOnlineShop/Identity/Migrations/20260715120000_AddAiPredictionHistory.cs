using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TeaOnlineShop.Identity.Migrations;

[DbContext(typeof(ApplicationIdentityContext))]
[Migration("20260715120000_AddAiPredictionHistory")]
public sealed class AddAiPredictionHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF OBJECT_ID(N'[dbo].[AiPredictionHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AiPredictionHistory]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_AiPredictionHistory] PRIMARY KEY,
        [PublicId] uniqueidentifier NOT NULL,
        [PredictionType] nvarchar(30) NOT NULL,
        [Grade] nvarchar(20) NOT NULL CONSTRAINT [DF_AiPredictionHistory_Grade] DEFAULT(N''),
        [HorizonDays] int NOT NULL,
        [RequestedByUserId] int NULL,
        [RequestedByName] nvarchar(120) NOT NULL,
        [RequestedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_AiPredictionHistory_RequestedAtUtc] DEFAULT(SYSUTCDATETIME()),
        [Model] nvarchar(120) NOT NULL,
        [ModelVersion] nvarchar(40) NOT NULL CONSTRAINT [DF_AiPredictionHistory_ModelVersion] DEFAULT(N''),
        [Strategy] nvarchar(80) NOT NULL CONSTRAINT [DF_AiPredictionHistory_Strategy] DEFAULT(N''),
        [ExpectedMape] decimal(9,4) NULL,
        [DataSource] nvarchar(50) NOT NULL,
        [SourceLabel] nvarchar(160) NOT NULL,
        [SourceNote] nvarchar(1000) NOT NULL CONSTRAINT [DF_AiPredictionHistory_SourceNote] DEFAULT(N''),
        [SourceStartDateUtc] datetime2 NULL,
        [SourceEndDateUtc] datetime2 NULL,
        [InputSummary] nvarchar(1000) NOT NULL,
        [ResultJson] nvarchar(max) NOT NULL,
        [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_AiPredictionHistory_Status] DEFAULT(N'Succeeded')
    );
    CREATE UNIQUE INDEX [UX_AiPredictionHistory_PublicId] ON [dbo].[AiPredictionHistory]([PublicId]);
    CREATE INDEX [IX_AiPredictionHistory_Type_Requested] ON [dbo].[AiPredictionHistory]([PredictionType], [RequestedAtUtc]);
    CREATE INDEX [IX_AiPredictionHistory_Grade_Requested] ON [dbo].[AiPredictionHistory]([Grade], [RequestedAtUtc]);
END;

EXEC(N'CREATE OR ALTER TRIGGER [dbo].[TR_AiPredictionHistory_Immutable]
ON [dbo].[AiPredictionHistory]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51003, ''AI prediction history is append-only and cannot be modified or deleted.'', 1;
END');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'[dbo].[TR_AiPredictionHistory_Immutable]', N'TR') IS NOT NULL DROP TRIGGER [dbo].[TR_AiPredictionHistory_Immutable];
IF OBJECT_ID(N'[dbo].[AiPredictionHistory]', N'U') IS NOT NULL DROP TABLE [dbo].[AiPredictionHistory];
""");
    }
}
