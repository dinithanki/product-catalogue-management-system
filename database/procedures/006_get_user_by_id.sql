
USE KeellsProductCatalogue;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Users_GetById
    @UserId INT
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
    WHERE UserId = @UserId;
END;
GO
