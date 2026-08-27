-- Check if tables already exist and create them if not
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Supplier')
BEGIN
    CREATE TABLE Supplier (
        Id INT PRIMARY KEY IDENTITY(1,1),
        SupplierCode VARCHAR(50) NOT NULL,
        Name VARCHAR(100) NOT NULL,
        ContactPerson VARCHAR(100),
        Phone VARCHAR(50),
        Email VARCHAR(100),
        Address VARCHAR(200),
        RegistrationDate DATETIME NOT NULL DEFAULT GETDATE(),
        QRCodeData VARCHAR(255) NOT NULL,
        Status VARCHAR(20) DEFAULT 'Active',
        Notes VARCHAR(500)
    );
    PRINT 'Supplier table created.';
END
ELSE
BEGIN
    PRINT 'Supplier table already exists.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SupplierCategory')
BEGIN
    CREATE TABLE SupplierCategory (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name VARCHAR(100) NOT NULL,
        Description VARCHAR(500)
    );
    PRINT 'SupplierCategory table created.';
END
ELSE
BEGIN
    PRINT 'SupplierCategory table already exists.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SupplierCategoryMapping')
BEGIN
    CREATE TABLE SupplierCategoryMapping (
        Id INT PRIMARY KEY IDENTITY(1,1),
        SupplierId INT NOT NULL,
        CategoryId INT NOT NULL,
        CONSTRAINT FK_SupplierCategoryMapping_Supplier FOREIGN KEY (SupplierId) REFERENCES Supplier(Id),
        CONSTRAINT FK_SupplierCategoryMapping_Category FOREIGN KEY (CategoryId) REFERENCES SupplierCategory(Id)
    );
    PRINT 'SupplierCategoryMapping table created.';
END
ELSE
BEGIN
    PRINT 'SupplierCategoryMapping table already exists.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SupplyItem')
BEGIN
    CREATE TABLE SupplyItem (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Name VARCHAR(100) NOT NULL,
        Category VARCHAR(50) NOT NULL,
        Unit VARCHAR(20) NOT NULL,
        Description VARCHAR(500),
        MinimumStock DECIMAL(10,2),
        CurrentStock DECIMAL(10,2) DEFAULT 0
    );
    PRINT 'SupplyItem table created.';
END
ELSE
BEGIN
    PRINT 'SupplyItem table already exists.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Delivery')
BEGIN
    CREATE TABLE Delivery (
        Id INT PRIMARY KEY IDENTITY(1,1),
        DeliveryCode VARCHAR(50) NOT NULL,
        SupplierId INT NOT NULL,
        ReceivedById INT NOT NULL,
        DeliveryDate DATETIME NOT NULL DEFAULT GETDATE(),
        TotalAmount DECIMAL(10,2),
        Status VARCHAR(20) DEFAULT 'Received',
        Notes VARCHAR(500),
        CONSTRAINT FK_Delivery_Supplier FOREIGN KEY (SupplierId) REFERENCES Supplier(Id),
        CONSTRAINT FK_Delivery_User FOREIGN KEY (ReceivedById) REFERENCES [User](Id)
    );
    PRINT 'Delivery table created.';
END
ELSE
BEGIN
    PRINT 'Delivery table already exists.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DeliveryItem')
BEGIN
    CREATE TABLE DeliveryItem (
        Id INT PRIMARY KEY IDENTITY(1,1),
        DeliveryId INT NOT NULL,
        ItemId INT NOT NULL,
        Quantity DECIMAL(10,2) NOT NULL,
        UnitPrice DECIMAL(10,2),
        TotalPrice DECIMAL(10,2),
        Notes VARCHAR(500),
        CONSTRAINT FK_DeliveryItem_Delivery FOREIGN KEY (DeliveryId) REFERENCES Delivery(Id),
        CONSTRAINT FK_DeliveryItem_SupplyItem FOREIGN KEY (ItemId) REFERENCES SupplyItem(Id)
    );
    PRINT 'DeliveryItem table created.';
END
ELSE
BEGIN
    PRINT 'DeliveryItem table already exists.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRCodeScan')
BEGIN
    CREATE TABLE QRCodeScan (
        Id INT PRIMARY KEY IDENTITY(1,1),
        QRCodeData VARCHAR(255) NOT NULL,
        ScannedById INT NOT NULL,
        ScanDateTime DATETIME NOT NULL DEFAULT GETDATE(),
        ScanResult VARCHAR(50),
        ActionTaken VARCHAR(50),
        Notes VARCHAR(500),
        CONSTRAINT FK_QRCodeScan_User FOREIGN KEY (ScannedById) REFERENCES [User](Id)
    );
    PRINT 'QRCodeScan table created.';
END
ELSE
BEGIN
    PRINT 'QRCodeScan table already exists.';
END

-- Show tables structure for verification
SELECT 
    t.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'YES' ELSE 'NO' END AS IS_PRIMARY_KEY
FROM 
    INFORMATION_SCHEMA.TABLES t
INNER JOIN 
    INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME
LEFT JOIN (
    SELECT 
        k.TABLE_NAME, 
        k.COLUMN_NAME
    FROM 
        INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN 
        INFORMATION_SCHEMA.KEY_COLUMN_USAGE k ON tc.CONSTRAINT_NAME = k.CONSTRAINT_NAME
    WHERE 
        tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
WHERE 
    t.TABLE_NAME IN ('Supplier', 'SupplierCategory', 'SupplierCategoryMapping', 'SupplyItem', 'Delivery', 'DeliveryItem', 'QRCodeScan')
ORDER BY 
    t.TABLE_NAME, 
    c.ORDINAL_POSITION; 