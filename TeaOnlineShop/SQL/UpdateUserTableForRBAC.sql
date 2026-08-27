-- Check if UserRole column exists in User table
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'User' AND COLUMN_NAME = 'UserRole'
)
BEGIN
    -- Add UserRole column
    ALTER TABLE [User] ADD UserRole NVARCHAR(50) NULL;
    
    -- Set default role for existing users
    -- Set admin users to 'admin' role
    UPDATE [User] SET UserRole = 'admin' WHERE IfAdmin = 1;
    
    -- Set regular users to 'Customer' role
    UPDATE [User] SET UserRole = 'Customer' WHERE UserRole IS NULL OR UserRole = '';
    
    -- Set the column to NOT NULL with default value
    ALTER TABLE [User] ALTER COLUMN UserRole NVARCHAR(50) NOT NULL;
    
    PRINT 'UserRole column added to User table successfully';
END
ELSE
BEGIN
    PRINT 'UserRole column already exists in User table';
END 