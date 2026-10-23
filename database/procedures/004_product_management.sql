
USE KeellsProductCatalogue;
GO

-- Status codes:
-- 0 = Success
-- 1 = Invalid input
-- 2 = Product not found
-- 3 = Actor is not authorized
-- 4 = Supplier not found or inactive
-- 5 = Duplicate SKU (the repository maps the unique constraint violation)

-- 1. Get products with server-side pagination, search,
-- supplier/category filtering, and sorting.
-- Returns total count as the first result set and products
-- as the second result set (Dapper QueryMultiple).
CREATE OR ALTER PROCEDURE dbo.usp_Products_GetPaged
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @Search NVARCHAR(100) = NULL,
    @SupplierId INT = NULL,
    @Category NVARCHAR(100) = NULL,
    @SortBy NVARCHAR(20) = 'name',
    @SortDirection NVARCHAR(4) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1
       OR @PageSize < 1
       OR @PageSize > 100
       OR @SortBy NOT IN ('name', 'sku', 'category', 'price')
       OR @SortDirection NOT IN ('ASC', 'DESC')
    BEGIN
        THROW 50001, 'Invalid product query parameters.', 1;
    END;

    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), '');
    SET @Category = NULLIF(LTRIM(RTRIM(@Category)), '');

    SELECT COUNT_BIG(*) AS TotalCount
    FROM dbo.Products p
    INNER JOIN dbo.Suppliers s ON s.SupplierId = p.SupplierId
    WHERE p.IsDeleted = 0
      AND (@Search IS NULL
           OR p.Name LIKE '%' + @Search + '%'
           OR p.SKU LIKE '%' + @Search + '%')
      AND (@SupplierId IS NULL OR p.SupplierId = @SupplierId)
      AND (@Category IS NULL OR p.Category = @Category);

    SELECT
        p.ProductId,
        p.SKU,
        p.Name,
        p.Category,
        p.SupplierId,
        s.Name AS SupplierName,
        s.CurrencyCode,
        p.UnitPrice,
        p.PackSize
    FROM dbo.Products p
    INNER JOIN dbo.Suppliers s ON s.SupplierId = p.SupplierId
    WHERE p.IsDeleted = 0
      AND (@Search IS NULL
           OR p.Name LIKE '%' + @Search + '%'
           OR p.SKU LIKE '%' + @Search + '%')
      AND (@SupplierId IS NULL OR p.SupplierId = @SupplierId)
      AND (@Category IS NULL OR p.Category = @Category)
    ORDER BY
        CASE WHEN @SortBy = 'name'
                  AND @SortDirection = 'ASC'
             THEN p.Name END ASC,
        CASE WHEN @SortBy = 'name'
                  AND @SortDirection = 'DESC'
             THEN p.Name END DESC,
        CASE WHEN @SortBy = 'sku'
                  AND @SortDirection = 'ASC'
             THEN p.SKU END ASC,
        CASE WHEN @SortBy = 'sku'
                  AND @SortDirection = 'DESC'
             THEN p.SKU END DESC,
        CASE WHEN @SortBy = 'category'
                  AND @SortDirection = 'ASC'
             THEN p.Category END ASC,
        CASE WHEN @SortBy = 'category'
                  AND @SortDirection = 'DESC'
             THEN p.Category END DESC,
        CASE WHEN @SortBy = 'price'
                  AND @SortDirection = 'ASC'
             THEN p.UnitPrice END ASC,
        CASE WHEN @SortBy = 'price'
                  AND @SortDirection = 'DESC'
             THEN p.UnitPrice END DESC,
        p.ProductId ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- 2. Get a single product and its supplier details.
CREATE OR ALTER PROCEDURE dbo.usp_Products_GetById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.ProductId,
        p.SKU,
        p.Name,
        p.Category,
        p.SupplierId,
        s.Name AS SupplierName,
        s.CurrencyCode,
        p.UnitPrice,
        p.PackSize
    FROM dbo.Products p
    INNER JOIN dbo.Suppliers s ON s.SupplierId = p.SupplierId
    WHERE p.ProductId = @ProductId
      AND p.IsDeleted = 0;
END;
GO

-- 3. Create a product.
-- The backend must require authentication and validate inputs.
CREATE OR ALTER PROCEDURE dbo.usp_Products_Create
    @PerformedByUserId INT,
    @SKU NVARCHAR(50),
    @Name NVARCHAR(150),
    @Category NVARCHAR(100),
    @SupplierId INT,
    @UnitPrice DECIMAL(18,4),
    @PackSize NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND IsActive = 1
    )
    BEGIN
        SELECT 3 AS StatusCode;
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@SKU)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Category)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@PackSize)), '') IS NULL
       OR @UnitPrice < 0
    BEGIN
        SELECT 1 AS StatusCode;
        RETURN;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Suppliers
        WHERE SupplierId = @SupplierId
          AND IsActive = 1
    )
    BEGIN
        SELECT 4 AS StatusCode;
        RETURN;
    END;

    INSERT INTO dbo.Products
        (SKU, Name, Category, SupplierId, UnitPrice, PackSize)
    VALUES
        (TRIM(@SKU), TRIM(@Name), TRIM(@Category),
         @SupplierId, @UnitPrice, TRIM(@PackSize));

    SELECT
        0 AS StatusCode,
        CONVERT(INT, SCOPE_IDENTITY()) AS ProductId;
END;
GO

-- 4. Update an existing product.
CREATE OR ALTER PROCEDURE dbo.usp_Products_Update
    @PerformedByUserId INT,
    @ProductId INT,
    @SKU NVARCHAR(50),
    @Name NVARCHAR(150),
    @Category NVARCHAR(100),
    @SupplierId INT,
    @UnitPrice DECIMAL(18,4),
    @PackSize NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND IsActive = 1
    )
    BEGIN
        SELECT 3 AS StatusCode;
        RETURN;
    END;

    IF NULLIF(LTRIM(RTRIM(@SKU)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@Category)), '') IS NULL
       OR NULLIF(LTRIM(RTRIM(@PackSize)), '') IS NULL
       OR @UnitPrice < 0
    BEGIN
        SELECT 1 AS StatusCode;
        RETURN;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Suppliers
        WHERE SupplierId = @SupplierId
          AND IsActive = 1
    )
    BEGIN
        SELECT 4 AS StatusCode;
        RETURN;
    END;

    UPDATE dbo.Products
    SET
        SKU = TRIM(@SKU),
        Name = TRIM(@Name),
        Category = TRIM(@Category),
        SupplierId = @SupplierId,
        UnitPrice = @UnitPrice,
        PackSize = TRIM(@PackSize)
    WHERE ProductId = @ProductId
      AND IsDeleted = 0;

    IF @@ROWCOUNT = 0
        SELECT 2 AS StatusCode;
    ELSE
        SELECT 0 AS StatusCode;
END;
GO

-- 5. Soft-delete a product (Admin only).
CREATE OR ALTER PROCEDURE dbo.usp_Products_SoftDelete
    @PerformedByUserId INT,
    @ProductId INT
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

    UPDATE dbo.Products
    SET IsDeleted = 1
    WHERE ProductId = @ProductId
      AND IsDeleted = 0;

    IF @@ROWCOUNT = 0
        SELECT 2 AS StatusCode;
    ELSE
        SELECT 0 AS StatusCode;
END;
GO
