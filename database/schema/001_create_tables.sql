
USE KeellsProductCatalogue;
GO

-- =====================================================
-- 1. USERS
-- =====================================================
CREATE TABLE dbo.Users
(
    UserId INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(10) NOT NULL,

    MustChangePassword BIT NOT NULL
        CONSTRAINT DF_Users_MustChangePassword DEFAULT (0),

    IsActive BIT NOT NULL
        CONSTRAINT DF_Users_IsActive DEFAULT (1),

    CONSTRAINT PK_Users
        PRIMARY KEY (UserId),

    CONSTRAINT UQ_Users_Username
        UNIQUE (Username),

    CONSTRAINT CK_Users_Role
        CHECK (Role IN ('Admin', 'User'))
);
GO


-- =====================================================
-- 2. SUPPLIERS
-- =====================================================
CREATE TABLE dbo.Suppliers
(
    SupplierId INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Country NVARCHAR(100) NOT NULL,
    CurrencyCode CHAR(3) NOT NULL,
    ContactNumber NVARCHAR(30) NOT NULL,
    Email NVARCHAR(254) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),

    CONSTRAINT PK_Suppliers
        PRIMARY KEY (SupplierId)
);
GO


-- =====================================================
-- 3. PRODUCTS
-- =====================================================
CREATE TABLE dbo.Products
(
    ProductId INT IDENTITY(1,1) NOT NULL,
    SKU NVARCHAR(50) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    SupplierId INT NOT NULL,
    UnitPrice DECIMAL(18,4) NOT NULL,
    PackSize NVARCHAR(50) NOT NULL,

    IsDeleted BIT NOT NULL
        CONSTRAINT DF_Products_IsDeleted DEFAULT (0),

    CONSTRAINT PK_Products
        PRIMARY KEY (ProductId),

    CONSTRAINT UQ_Products_SKU
        UNIQUE (SKU),

    CONSTRAINT FK_Products_Suppliers
        FOREIGN KEY (SupplierId)
        REFERENCES dbo.Suppliers(SupplierId),

    CONSTRAINT CK_Products_UnitPrice
        CHECK (UnitPrice >= 0)
);
GO


-- =====================================================
-- 4. REFRESH TOKENS
-- Stores hashed refresh tokens, never plaintext tokens.
-- =====================================================
CREATE TABLE dbo.RefreshTokens
(
    RefreshTokenId BIGINT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,
    TokenHash CHAR(64) NOT NULL,
    TokenFamilyId UNIQUEIDENTIFIER NOT NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_RefreshTokens_CreatedAt
        DEFAULT (SYSUTCDATETIME()),

    ExpiresAt DATETIME2(0) NOT NULL,
    RevokedAt DATETIME2(0) NULL,
    ReplacedByTokenId BIGINT NULL,

    CONSTRAINT PK_RefreshTokens
        PRIMARY KEY (RefreshTokenId),

    CONSTRAINT UQ_RefreshTokens_TokenHash
        UNIQUE (TokenHash),

    CONSTRAINT FK_RefreshTokens_Users
        FOREIGN KEY (UserId)
        REFERENCES dbo.Users(UserId),

    CONSTRAINT FK_RefreshTokens_ReplacedBy
        FOREIGN KEY (ReplacedByTokenId)
        REFERENCES dbo.RefreshTokens(RefreshTokenId),

    CONSTRAINT CK_RefreshTokens_Expiry
        CHECK (ExpiresAt > CreatedAt)
);
GO


-- =====================================================
-- 5. PRODUCT CATALOGUE INDEXES
-- Supports supplier/category filtering for active products.
-- =====================================================
CREATE INDEX IX_Products_Active_Supplier_Category
ON dbo.Products (SupplierId, Category)
INCLUDE (SKU, Name, UnitPrice, PackSize)
WHERE IsDeleted = 0;
GO


-- =====================================================
-- 6. REFRESH TOKEN INDEXES
-- =====================================================
CREATE INDEX IX_RefreshTokens_UserId_FamilyId
ON dbo.RefreshTokens (UserId, TokenFamilyId);
GO

CREATE INDEX IX_RefreshTokens_ExpiresAt
ON dbo.RefreshTokens (ExpiresAt);
GO
