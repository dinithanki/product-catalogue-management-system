
USE KeellsProductCatalogue;
GO

-- Status codes:
-- 0 = Success
-- 1 = Invalid input
-- 2 = Supplier not found
-- 3 = Actor is not an active Admin

-- 1. List all suppliers (including inactive suppliers).
-- The backend must require authentication.
CREATE OR ALTER PROCEDURE dbo.usp_Suppliers_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SupplierId,
        Name,
        Country,
        CurrencyCode,
        ContactNumber,
        Email,
        IsActive
    FROM dbo.Suppliers
    ORDER BY Name;
END;
GO

-- 2. Get active suppliers for the product form dropdown.
CREATE OR ALTER PROCEDURE dbo.usp_Suppliers_GetActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SupplierId,
        Name,
        Country,
        CurrencyCode
    FROM dbo.Suppliers
    WHERE IsActive = 1
    ORDER BY Name;
END;
GO

-- 3. Create a supplier (Admin only).
CREATE OR ALTER PROCEDURE dbo.usp_Suppliers_Create
    @PerformedByUserId INT,
    @Name NVARCHAR(150),
    @Country NVARCHAR(100),
    @CurrencyCode CHAR(3),
    @ContactNumber NVARCHAR(30),
    @Email NVARCHAR(254)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND Role = 'Admin'
          AND IsActive = 1
    )
    BEGIN
        SELECT 3 AS StatusCode;
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Country)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@CurrencyCode)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@ContactNumber)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Email)), '') IS NULL
    BEGIN
        SELECT 1 AS StatusCode;
        RETURN;
    END;

    INSERT INTO dbo.Suppliers
        (Name, Country, CurrencyCode, ContactNumber, Email, IsActive)
    VALUES
        (TRIM(@Name), TRIM(@Country), UPPER(TRIM(@CurrencyCode)),
         TRIM(@ContactNumber), TRIM(@Email), 1);

    SELECT
        0 AS StatusCode,
        CONVERT(INT, SCOPE_IDENTITY()) AS SupplierId;
END;
GO

-- 4. Update supplier details (Admin only).
CREATE OR ALTER PROCEDURE dbo.usp_Suppliers_Update
    @PerformedByUserId INT,
    @SupplierId INT,
    @Name NVARCHAR(150),
    @Country NVARCHAR(100),
    @CurrencyCode CHAR(3),
    @ContactNumber NVARCHAR(30),
    @Email NVARCHAR(254)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND Role = 'Admin'
          AND IsActive = 1
    )
    BEGIN
        SELECT 3 AS StatusCode;
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Country)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@CurrencyCode)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@ContactNumber)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Email)), '') IS NULL
    BEGIN
        SELECT 1 AS StatusCode;
        RETURN;
    END;

    UPDATE dbo.Suppliers
    SET
        Name = TRIM(@Name),
        Country = TRIM(@Country),
        CurrencyCode = UPPER(TRIM(@CurrencyCode)),
        ContactNumber = TRIM(@ContactNumber),
        Email = TRIM(@Email)
    WHERE SupplierId = @SupplierId;

    IF @@ROWCOUNT = 0
        SELECT 2 AS StatusCode;
    ELSE
        SELECT 0 AS StatusCode;
END;
GO

-- 5. Deactivate a supplier (Admin only).
-- Existing products remain linked to the supplier.
CREATE OR ALTER PROCEDURE dbo.usp_Suppliers_Deactivate
    @PerformedByUserId INT,
    @SupplierId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND Role = 'Admin'
          AND IsActive = 1
    )
    BEGIN
        SELECT 3 AS StatusCode;
        RETURN;
    END;

    UPDATE dbo.Suppliers
    SET IsActive = 0
    WHERE SupplierId = @SupplierId;

    IF @@ROWCOUNT = 0
        SELECT 2 AS StatusCode;
    ELSE
        SELECT 0 AS StatusCode;
END;
GO

-- 6. Reactivate a supplier (Admin only).
CREATE OR ALTER PROCEDURE dbo.usp_Suppliers_Reactivate
    @PerformedByUserId INT,
    @SupplierId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND Role = 'Admin'
          AND IsActive = 1
    )
    BEGIN
        SELECT 3 AS StatusCode;
        RETURN;
    END;

    UPDATE dbo.Suppliers
    SET IsActive = 1
    WHERE SupplierId = @SupplierId;

    IF @@ROWCOUNT = 0
        SELECT 2 AS StatusCode;
    ELSE
        SELECT 0 AS StatusCode;
END;
GO
