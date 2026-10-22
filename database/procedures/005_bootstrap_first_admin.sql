USE KeellsProductCatalogue;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Users_BootstrapFirstAdmin
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Lock the Users table while checking that no account exists.
        IF EXISTS (SELECT 1 FROM dbo.Users WITH (TABLOCKX, HOLDLOCK))
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 1 AS StatusCode;
            RETURN;
        END;

        IF NULLIF(LTRIM(RTRIM(@Username)), '') IS NULL
           OR NULLIF(@PasswordHash, '') IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 2 AS StatusCode;
            RETURN;
        END;

        INSERT INTO dbo.Users
            (Username, PasswordHash, Role, MustChangePassword, IsActive)
        VALUES
            (@Username, @PasswordHash, 'Admin', 1, 1);

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
