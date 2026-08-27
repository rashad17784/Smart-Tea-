using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaOnlineShop.Identity.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleWarehouseAuditActors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.Delivery', 'ReceivedByName') IS NULL
                BEGIN
                    ALTER TABLE dbo.Delivery
                    ADD ReceivedByName nvarchar(120) NOT NULL
                        CONSTRAINT DF_Delivery_ReceivedByName DEFAULT N'';
                END;

                EXEC(N'UPDATE d
                    SET ReceivedByName = COALESCE(NULLIF(d.ReceivedByName, N''''), u.FullName, N''Legacy user'')
                    FROM dbo.Delivery d
                    LEFT JOIN dbo.[User] u ON u.Id = d.ReceivedById');

                DECLARE @deliveryUserFk sysname;
                SELECT TOP (1) @deliveryUserFk = fk.name
                FROM sys.foreign_keys fk
                JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Delivery')
                  AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = N'ReceivedById';

                IF @deliveryUserFk IS NOT NULL
                    EXEC(N'ALTER TABLE dbo.Delivery DROP CONSTRAINT [' + @deliveryUserFk + N']');

                IF OBJECT_ID(N'dbo.QRCodeScan', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('dbo.QRCodeScan', 'ScannedByName') IS NULL
                    BEGIN
                        ALTER TABLE dbo.QRCodeScan
                        ADD ScannedByName nvarchar(120) NOT NULL
                            CONSTRAINT DF_QRCodeScan_ScannedByName DEFAULT N'';
                    END;

                    EXEC(N'UPDATE s
                        SET ScannedByName = COALESCE(NULLIF(s.ScannedByName, N''''), u.FullName, N''Legacy user'')
                        FROM dbo.QRCodeScan s
                        LEFT JOIN dbo.[User] u ON u.Id = s.ScannedById');

                    DECLARE @scanUserFk sysname;
                    SELECT TOP (1) @scanUserFk = fk.name
                    FROM sys.foreign_keys fk
                    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.QRCodeScan')
                      AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = N'ScannedById';

                    IF @scanUserFk IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.QRCodeScan DROP CONSTRAINT [' + @scanUserFk + N']');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.Delivery', 'ReceivedByName') IS NOT NULL
                    ALTER TABLE dbo.Delivery DROP CONSTRAINT DF_Delivery_ReceivedByName;
                IF COL_LENGTH('dbo.Delivery', 'ReceivedByName') IS NOT NULL
                    ALTER TABLE dbo.Delivery DROP COLUMN ReceivedByName;

                IF OBJECT_ID(N'dbo.QRCodeScan', N'U') IS NOT NULL
                   AND COL_LENGTH('dbo.QRCodeScan', 'ScannedByName') IS NOT NULL
                    ALTER TABLE dbo.QRCodeScan DROP CONSTRAINT DF_QRCodeScan_ScannedByName;
                IF OBJECT_ID(N'dbo.QRCodeScan', N'U') IS NOT NULL
                   AND COL_LENGTH('dbo.QRCodeScan', 'ScannedByName') IS NOT NULL
                    ALTER TABLE dbo.QRCodeScan DROP COLUMN ScannedByName;
                """);
        }
    }
}
