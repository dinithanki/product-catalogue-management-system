
USE KeellsProductCatalogue;
GO

-- 1. Retrieve a user for login.
-- .NET verifies the submitted password against PasswordHash.
CREATE OR ALTER PROCEDURE dbo.usp_Users_GetByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UserId,
        Username,
        PasswordHash,
        Role,
        MustChangePassword,
        IsActive
    FROM dbo.Users
    WHERE Username = @Username;
END;
GO

-- 2. Save a new password hash.
-- .NET must verify the user's authorization and password policy.
CREATE OR ALTER PROCEDURE dbo.usp_Users_ChangePassword
    @UserId INT,
    @NewPasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET
        PasswordHash = @NewPasswordHash,
        MustChangePassword = 0
    WHERE UserId = @UserId
      AND IsActive = 1;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO

-- 3. Create a refresh-token record after successful login.
CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_Create
    @UserId INT,
    @TokenHash CHAR(64),
    @TokenFamilyId UNIQUEIDENTIFIER,
    @ExpiresAt DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Users
        WHERE UserId = @UserId
          AND IsActive = 1
    )
    BEGIN
        SELECT 1 AS StatusCode;
        RETURN;
    END;

    IF @ExpiresAt <= SYSUTCDATETIME()
    BEGIN
        SELECT 2 AS StatusCode;
        RETURN;
    END;

    INSERT INTO dbo.RefreshTokens
        (UserId, TokenHash, TokenFamilyId, ExpiresAt)
    VALUES
        (@UserId, @TokenHash, @TokenFamilyId, @ExpiresAt);

    SELECT 0 AS StatusCode;
END;
GO

-- 4. Rotate a refresh token atomically.
-- StatusCode:
-- 0 = success
-- 1 = token not found
-- 2 = token expired
-- 3 = invalid/revoked token
-- 4 = user inactive
CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_Rotate
    @CurrentTokenHash CHAR(64),
    @NewTokenHash CHAR(64),
    @NewExpiresAt DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @RefreshTokenId BIGINT,
        @UserId INT,
        @TokenFamilyId UNIQUEIDENTIFIER,
        @ExpiresAt DATETIME2(0),
        @RevokedAt DATETIME2(0),
        @ReplacedByTokenId BIGINT,
        @NewTokenId BIGINT,
        @StatusCode INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Lock the old token so simultaneous requests cannot
        -- both successfully rotate it.
        SELECT
            @RefreshTokenId = RefreshTokenId,
            @UserId = UserId,
            @TokenFamilyId = TokenFamilyId,
            @ExpiresAt = ExpiresAt,
            @RevokedAt = RevokedAt,
            @ReplacedByTokenId = ReplacedByTokenId
        FROM dbo.RefreshTokens WITH (UPDLOCK, HOLDLOCK)
        WHERE TokenHash = @CurrentTokenHash;

        IF @RefreshTokenId IS NULL
        BEGIN
            SET @StatusCode = 1;
        END
        ELSE IF @RevokedAt IS NOT NULL
        BEGIN
            -- Reuse of a rotated token may indicate theft.
            IF @ReplacedByTokenId IS NOT NULL
            BEGIN
                UPDATE dbo.RefreshTokens
                SET RevokedAt = COALESCE
                    (RevokedAt, SYSUTCDATETIME())
                WHERE TokenFamilyId = @TokenFamilyId;
            END;

            SET @StatusCode = 3;
        END
        ELSE IF @ExpiresAt <= SYSUTCDATETIME()
        BEGIN
            SET @StatusCode = 2;
        END
        ELSE IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
            WHERE UserId = @UserId
              AND IsActive = 1
        )
        BEGIN
            UPDATE dbo.RefreshTokens
            SET RevokedAt = COALESCE
                (RevokedAt, SYSUTCDATETIME())
            WHERE TokenFamilyId = @TokenFamilyId;

            SET @StatusCode = 4;
        END
        ELSE IF @NewExpiresAt <= SYSUTCDATETIME()
        BEGIN
            SET @StatusCode = 2;
        END
        ELSE
        BEGIN
            -- Create the replacement first.
            INSERT INTO dbo.RefreshTokens
                (UserId, TokenHash, TokenFamilyId, ExpiresAt)
            VALUES
                (@UserId, @NewTokenHash, @TokenFamilyId,
                 @NewExpiresAt);

            SET @NewTokenId = CONVERT(BIGINT, SCOPE_IDENTITY());

            -- Invalidate the old token and link its replacement.
            UPDATE dbo.RefreshTokens
            SET
                RevokedAt = SYSUTCDATETIME(),
                ReplacedByTokenId = @NewTokenId
            WHERE RefreshTokenId = @RefreshTokenId;

            SET @StatusCode = 0;
        END;

        COMMIT TRANSACTION;

        SELECT
            @StatusCode AS StatusCode,
            CASE WHEN @StatusCode = 0
                 THEN @UserId END AS UserId,
            CASE WHEN @StatusCode = 0
                 THEN @TokenFamilyId END AS TokenFamilyId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

-- 5. Revoke the complete login session using its token hash.
CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_RevokeCurrent
    @TokenHash CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TokenFamilyId UNIQUEIDENTIFIER;

    SELECT @TokenFamilyId = TokenFamilyId
    FROM dbo.RefreshTokens
    WHERE TokenHash = @TokenHash;

    IF @TokenFamilyId IS NOT NULL
    BEGIN
        UPDATE dbo.RefreshTokens
        SET RevokedAt = COALESCE
            (RevokedAt, SYSUTCDATETIME())
        WHERE TokenFamilyId = @TokenFamilyId;
    END;

    SELECT 0 AS StatusCode;
END;
GO

-- 6. Revoke all sessions for a user, e.g. after an
-- administrator resets their password or deactivates them.
CREATE OR ALTER PROCEDURE dbo.usp_RefreshTokens_RevokeAllForUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET RevokedAt = COALESCE
        (RevokedAt, SYSUTCDATETIME())
    WHERE UserId = @UserId;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO
