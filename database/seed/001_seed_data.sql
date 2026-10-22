
USE KeellsProductCatalogue;
GO

-- Sample suppliers
IF NOT EXISTS
(
    SELECT 1 FROM dbo.Suppliers
    WHERE Name = N'Global Rice Traders'
)
BEGIN
    INSERT INTO dbo.Suppliers
        (Name, Country, CurrencyCode, ContactNumber, Email, IsActive)
    VALUES
        (N'Global Rice Traders', N'India', 'INR',
         N'+91-9876543210', N'rice@example.com', 1);
END;

IF NOT EXISTS
(
    SELECT 1 FROM dbo.Suppliers
    WHERE Name = N'Pacific Electronics'
)
BEGIN
    INSERT INTO dbo.Suppliers
        (Name, Country, CurrencyCode, ContactNumber, Email, IsActive)
    VALUES
        (N'Pacific Electronics', N'China', 'CNY',
         N'+86-13800138000', N'electronics@example.com', 1);
END;

IF NOT EXISTS
(
    SELECT 1 FROM dbo.Suppliers
    WHERE Name = N'European Foods Ltd'
)
BEGIN
    INSERT INTO dbo.Suppliers
        (Name, Country, CurrencyCode, ContactNumber, Email, IsActive)
    VALUES
        (N'European Foods Ltd', N'United Kingdom', 'GBP',
         N'+44-7700900123', N'foods@example.com', 1);
END;
GO

-- Sample products
DECLARE @RiceSupplierId INT;
DECLARE @ElectronicsSupplierId INT;
DECLARE @FoodSupplierId INT;

SELECT @RiceSupplierId = SupplierId
FROM dbo.Suppliers
WHERE Name = N'Global Rice Traders';

SELECT @ElectronicsSupplierId = SupplierId
FROM dbo.Suppliers
WHERE Name = N'Pacific Electronics';

SELECT @FoodSupplierId = SupplierId
FROM dbo.Suppliers
WHERE Name = N'European Foods Ltd';

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'RICE-001')
BEGIN
    INSERT INTO dbo.Products
        (SKU, Name, Category, SupplierId, UnitPrice, PackSize)
    VALUES
        (N'RICE-001', N'Basmati Rice', N'Food',
         @RiceSupplierId, 250.0000, N'5 kg');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'ELEC-001')
BEGIN
    INSERT INTO dbo.Products
        (SKU, Name, Category, SupplierId, UnitPrice, PackSize)
    VALUES
        (N'ELEC-001', N'Electric Kettle', N'Electronics',
         @ElectronicsSupplierId, 85.5000, N'1 unit');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'FOOD-001')
BEGIN
    INSERT INTO dbo.Products
        (SKU, Name, Category, SupplierId, UnitPrice, PackSize)
    VALUES
        (N'FOOD-001', N'Biscuits', N'Food',
         @FoodSupplierId, 2.7500, N'Pack of 12');
END;
GO
