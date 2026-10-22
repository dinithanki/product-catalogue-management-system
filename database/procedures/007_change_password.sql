USE KeellsProductCatalogue;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Users_ChangePassword
    @UserId INT,
    @NewPasswordHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET PasswordHash = @NewPasswordHash,
        MustChangePassword = 0
    WHERE UserId = @UserId
      AND IsActive = 1;

    SELECT @@ROWCOUNT AS RowsAffected;
END;
GO
