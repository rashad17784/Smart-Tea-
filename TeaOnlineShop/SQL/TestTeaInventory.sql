-- First check if the tables exist
IF OBJECT_ID('dbo.TeaInventoryItems', 'U') IS NOT NULL
BEGIN
    PRINT 'TeaInventoryItems table exists';
    
    -- Check column structure
    SELECT 
        COLUMN_NAME, 
        DATA_TYPE, 
        CHARACTER_MAXIMUM_LENGTH, 
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TeaInventoryItems'
    ORDER BY ORDINAL_POSITION;
    
    -- Check if any records exist
    DECLARE @ItemCount INT;
    SELECT @ItemCount = COUNT(*) FROM dbo.TeaInventoryItems;
    PRINT 'Current record count in TeaInventoryItems: ' + CAST(@ItemCount AS VARCHAR);
    
    -- Insert a test record directly
    INSERT INTO dbo.TeaInventoryItems (
        Name, 
        TeaType, 
        Grade, 
        Origin, 
        BatchNumber, 
        Description, 
        CurrentStock, 
        Unit, 
        MinimumStock, 
        ReorderLevel, 
        ReorderQuantity, 
        UnitCost, 
        RetailPrice, 
        Status, 
        QRCodeData, 
        CreatedDate,
        HasBeenCorrected,
        LastCorrectedBy,
        CorrectionReason
    )
    VALUES (
        'SQL Test Green Tea', 
        'Green', 
        'Premium', 
        'China', 
        'TEST-BATCH-001', 
        'A test record inserted via SQL', 
        10.00, 
        'kg', 
        5.00, 
        3.00, 
        10.00, 
        15.00, 
        25.00, 
        'Active', 
        'INV-GRE-PRE-TEST-001-' + CAST(CAST(NEWID() AS VARBINARY(4)) AS VARCHAR(10)), 
        GETDATE(),
        0,
        '',
        ''
    );
    
    -- Check if the record was inserted
    SELECT * FROM dbo.TeaInventoryItems WHERE Name = 'SQL Test Green Tea';
END
ELSE
BEGIN
    PRINT 'WARNING: TeaInventoryItems table does not exist!';
END

-- Check transactions table
IF OBJECT_ID('dbo.TeaInventoryTransactions', 'U') IS NOT NULL
BEGIN
    PRINT 'TeaInventoryTransactions table exists';
    
    -- Check column structure
    SELECT 
        COLUMN_NAME, 
        DATA_TYPE, 
        CHARACTER_MAXIMUM_LENGTH, 
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'TeaInventoryTransactions'
    ORDER BY ORDINAL_POSITION;
    
    -- Check if any records exist
    DECLARE @TransCount INT;
    SELECT @TransCount = COUNT(*) FROM dbo.TeaInventoryTransactions;
    PRINT 'Current record count in TeaInventoryTransactions: ' + CAST(@TransCount AS VARCHAR);
END
ELSE
BEGIN
    PRINT 'WARNING: TeaInventoryTransactions table does not exist!';
END 