
USE KeellsProductCatalogue;
GO

-- Status codes:
-- 0 = Success
-- 1 = Request rejected
-- 2 = User not found
-- 3 = Last active Admin cannot be demoted/deactivated
-- 4 = Actor is not an active Admin
-- 5 = Cannot deactivate your own account

-- 1. Create a user with a temporary password.
-- .NET generates the password hash before calling this procedure.
CREATE OR ALTER PROCEDURE dbo.usp_Users_Create
    @PerformedByUserId INT,
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255),
    @Role NVARCHAR(10)
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

    IF @Role NOT IN ('Admin', 'User')
       OR NULLIF(LTRIM(RTRIM(@Username)), '') IS NULL
       OR NULLIF(@PasswordHash, '') IS NULL
    BEGIN
        SELECT 1 AS StatusCode;
        RETURN;
    END;

    INSERT INTO dbo.Users
        (Username, PasswordHash, Role, MustChangePassword, IsActive)
    VALUES
        (@Username, @PasswordHash, @Role, 1, 1);

    SELECT
        0 AS StatusCode,
        CONVERT(INT, SCOPE_IDENTITY()) AS UserId;
END;
GO

-- 2. List users without exposing password hashes.
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

-- 3. Edit a user's username and role.
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

        IF @Role NOT IN ('Admin', 'User')
           OR NULLIF(LTRIM(RTRIM(@Username)), '') IS NULL
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
               WHERE Role = 'Admin' AND IsActive = 1
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

-- 4. Reset a user's password.
-- The user must change it at next login.
-- Existing refresh-token sessions are revoked.
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
        SET RevokedAt = COALESCE(RevokedAt, SYSUTCDATETIME())
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

-- 5. Activate a user account.
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

-- 6. Deactivate a user account.
-- Prevent self-deactivation and protect the last active Admin.
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
               WHERE Role = 'Admin' AND IsActive = 1
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
        SET RevokedAt = COALESCE(RevokedAt, SYSUTCDATETIME())
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
