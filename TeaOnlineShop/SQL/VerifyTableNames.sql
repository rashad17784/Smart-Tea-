-- Script to verify correct table names and structure

-- Check what tables actually exist in the database
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Specifically check for our inventory tables
IF OBJECT_ID('dbo.TeaInventoryItems', 'U') IS NOT NULL
    PRINT 'Found table: TeaInventoryItems (correct name)';
ELSE
    PRINT 'WARNING: TeaInventoryItems table not found!';

IF OBJECT_ID('dbo.TeaInventoryItem', 'U') IS NOT NULL
    PRINT 'Found table: TeaInventoryItem (incorrect singular name)';
ELSE
    PRINT 'Table TeaInventoryItem does not exist (this is good if using plural naming)';

IF OBJECT_ID('dbo.TeaInventoryTransactions', 'U') IS NOT NULL
    PRINT 'Found table: TeaInventoryTransactions (correct name)';
ELSE
    PRINT 'WARNING: TeaInventoryTransactions table not found!';

IF OBJECT_ID('dbo.TeaInventoryTransaction', 'U') IS NOT NULL
    PRINT 'Found table: TeaInventoryTransaction (incorrect singular name)';
ELSE
    PRINT 'Table TeaInventoryTransaction does not exist (this is good if using plural naming)';

-- Check column structure for the inventory tables if they exist
IF OBJECT_ID('dbo.TeaInventoryItems', 'U') IS NOT NULL
BEGIN
    PRINT '-- TeaInventoryItems columns --';
    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TeaInventoryItems'
    ORDER BY ORDINAL_POSITION;
END

IF OBJECT_ID('dbo.TeaInventoryTransactions', 'U') IS NOT NULL
BEGIN
    PRINT '-- TeaInventoryTransactions columns --';
    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TeaInventoryTransactions'
    ORDER BY ORDINAL_POSITION;
END 