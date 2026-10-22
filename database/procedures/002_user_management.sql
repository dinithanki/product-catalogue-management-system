
USE KeellsProductCatalogue;
GO

-- Status codes:
-- 0 = Success
-- 1 = Invalid input
-- 2 = Username already exists (Create)
--     or User not found (other procedures)
-- 3 = Last active Admin cannot be demoted/deactivated
-- 4 = Actor is not an active Admin
-- 5 = Cannot deactivate your own account

-- =========================================================
-- 1. CREATE USER
-- =========================================================
CREATE OR ALTER PROCEDURE dbo.usp_Users_Create
    @PerformedByUserId INT,
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255),
    @Role NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND Role = 'Admin'
          AND IsActive = 1
    )
    BEGIN
        SELECT 4 AS StatusCode, CAST(NULL AS INT) AS UserId;
        RETURN;
    END;

    SET @Username = LTRIM(RTRIM(@Username));

    IF @Role NOT IN ('Admin', 'User')
       OR NULLIF(@Username, '') IS NULL
       OR NULLIF(@PasswordHash, '') IS NULL
    BEGIN
        SELECT 1 AS StatusCode, CAST(NULL AS INT) AS UserId;
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check whether the username already exists.
        IF EXISTS
        (
            SELECT 1
            FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
            WHERE Username = @Username
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 2 AS StatusCode, CAST(NULL AS INT) AS UserId;
            RETURN;
        END;

        INSERT INTO dbo.Users
        (
            Username,
            PasswordHash,
            Role,
            MustChangePassword,
            IsActive
        )
        VALUES
        (
            @Username,
            @PasswordHash,
            @Role,
            1,
            1
        );

        DECLARE @NewUserId INT = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        SELECT 0 AS StatusCode, @NewUserId AS UserId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        -- The unique constraint remains the final safeguard
        -- against duplicate usernames.
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            SELECT 2 AS StatusCode, CAST(NULL AS INT) AS UserId;
            RETURN;
        END;

        THROW;
    END CATCH;
END;
GO

-- =========================================================
-- 2. GET ALL USERS
-- Password hashes are never returned.
-- =========================================================
CREATE OR ALTER PROCEDURE dbo.usp_Users_GetAll
    @PerformedByUserId INT
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
        SELECT 4 AS StatusCode;
        RETURN;
    END;

    SELECT
        UserId,
        Username,
        Role,
        MustChangePassword,
        IsActive
    FROM dbo.Users
    ORDER BY Username;
END;
GO

-- =========================================================
-- 3. UPDATE USER
-- Prevents demotion of the last active Admin.
-- =========================================================
CREATE OR ALTER PROCEDURE dbo.usp_Users_Update
    @PerformedByUserId INT,
    @UserId INT,
    @Username NVARCHAR(50),
    @Role NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
            WHERE UserId = @PerformedByUserId
              AND Role = 'Admin'
              AND IsActive = 1
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 4 AS StatusCode;
            RETURN;
        END;

        SET @Username = LTRIM(RTRIM(@Username));

        IF @Role NOT IN ('Admin', 'User')
           OR NULLIF(@Username, '') IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 1 AS StatusCode;
            RETURN;
        END;

        DECLARE @CurrentRole NVARCHAR(10);
        DECLARE @CurrentActive BIT;

        SELECT
            @CurrentRole = Role,
            @CurrentActive = IsActive
        FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
        WHERE UserId = @UserId;

        IF @CurrentRole IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 2 AS StatusCode;
            RETURN;
        END;

        IF @CurrentRole = 'Admin'
           AND @CurrentActive = 1
           AND @Role = 'User'
           AND
           (
               SELECT COUNT_BIG(*)
               FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
               WHERE Role = 'Admin'
                 AND IsActive = 1
           ) <= 1
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 3 AS StatusCode;
            RETURN;
        END;

        UPDATE dbo.Users
        SET Username = @Username,
            Role = @Role
        WHERE UserId = @UserId;

        COMMIT TRANSACTION;

        SELECT 0 AS StatusCode;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

-- =========================================================
-- 4. RESET USER PASSWORD
-- Requires the user to change the password at next login.
-- Revokes existing refresh tokens.
-- =========================================================
CREATE OR ALTER PROCEDURE dbo.usp_Users_ResetPassword
    @PerformedByUserId INT,
    @UserId INT,
    @NewPasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @PerformedByUserId
          AND Role = 'Admin'
          AND IsActive = 1
    )
    BEGIN
        SELECT 4 AS StatusCode;
        RETURN;
    END;

    IF NULLIF(@NewPasswordHash, '') IS NULL
    BEGIN
        SELECT 1 AS StatusCode;
        RETURN;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.Users
        SET PasswordHash = @NewPasswordHash,
            MustChangePassword = 1
        WHERE UserId = @UserId;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 2 AS StatusCode;
            RETURN;
        END;

        UPDATE dbo.RefreshTokens
        SET RevokedAt = COALESCE(
            RevokedAt,
            SYSUTCDATETIME()
        )
        WHERE UserId = @UserId;

        COMMIT TRANSACTION;

        SELECT 0 AS StatusCode;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

-- =========================================================
-- 5. ACTIVATE USER
-- =========================================================
CREATE OR ALTER PROCEDURE dbo.usp_Users_Activate
    @PerformedByUserId INT,
    @UserId INT
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
        SELECT 4 AS StatusCode;
        RETURN;
    END;

    UPDATE dbo.Users
    SET IsActive = 1
    WHERE UserId = @UserId;

    IF @@ROWCOUNT = 0
        SELECT 2 AS StatusCode;
    ELSE
        SELECT 0 AS StatusCode;
END;
GO

-- =========================================================
-- 6. DEACTIVATE USER
-- Prevents self-deactivation and deactivation of the last
-- active Admin. Revokes the target user's refresh tokens.
-- =========================================================
CREATE OR ALTER PROCEDURE dbo.usp_Users_Deactivate
    @PerformedByUserId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
            WHERE UserId = @PerformedByUserId
              AND Role = 'Admin'
              AND IsActive = 1
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 4 AS StatusCode;
            RETURN;
        END;

        IF @PerformedByUserId = @UserId
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 5 AS StatusCode;
            RETURN;
        END;

        DECLARE @TargetRole NVARCHAR(10);
        DECLARE @TargetActive BIT;

        SELECT
            @TargetRole = Role,
            @TargetActive = IsActive
        FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
        WHERE UserId = @UserId;

        IF @TargetRole IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 2 AS StatusCode;
            RETURN;
        END;

        IF @TargetRole = 'Admin'
           AND @TargetActive = 1
           AND
           (
               SELECT COUNT_BIG(*)
               FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
               WHERE Role = 'Admin'
                 AND IsActive = 1
           ) <= 1
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 3 AS StatusCode;
            RETURN;
        END;

        UPDATE dbo.Users
        SET IsActive = 0
        WHERE UserId = @UserId;

        UPDATE dbo.RefreshTokens
        SET RevokedAt = COALESCE(
            RevokedAt,
            SYSUTCDATETIME()
        )
        WHERE UserId = @UserId;

        COMMIT TRANSACTION;

        SELECT 0 AS StatusCode;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO
