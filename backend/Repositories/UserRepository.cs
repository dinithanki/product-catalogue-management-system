
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace backend.Repositories;

public sealed class UserRepository(SqlConnection connection)
    : IUserRepository
{
    public async Task<(int StatusCode, int? UserId)> CreateUserAsync(
        int performedByUserId,
        string username,
        string passwordHash,
        string role)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@Username", username, DbType.String, size: 50);
        parameters.Add("@PasswordHash", passwordHash, DbType.String, size: 255);
        parameters.Add("@Role", role, DbType.String, size: 10);

        var result = await connection.QuerySingleAsync<CreateUserResult>(
            "dbo.usp_Users_Create",
            parameters,
            commandType: CommandType.StoredProcedure);

        return (result.StatusCode, result.UserId);
    }

    public async Task<int> UpdateUserAsync(
        int performedByUserId,
        int userId,
        string username,
        string role)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@Username", username, DbType.String, size: 50);
        parameters.Add("@Role", role, DbType.String, size: 10);

        try
        {
            return await connection.QuerySingleAsync<int>(
                "dbo.usp_Users_Update",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException exception) when (
            (exception.Number == 2601 || exception.Number == 2627) &&
            exception.Message.Contains(
                "UQ_Users_Username",
                StringComparison.OrdinalIgnoreCase))
        {
            // A concurrent update can still win after any pre-check, so map
            // the database constraint violation to the API conflict status.
            return 6;
        }
    }

    public async Task<int> ResetUserPasswordAsync(
        int performedByUserId,
        int userId,
        string newPasswordHash)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add("@NewPasswordHash", newPasswordHash, DbType.String, size: 255);

        return await connection.QuerySingleAsync<int>(
            "dbo.usp_Users_ResetPassword",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> ActivateUserAsync(
        int performedByUserId,
        int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@UserId", userId, DbType.Int32);

        return await connection.QuerySingleAsync<int>(
            "dbo.usp_Users_Activate",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> DeactivateUserAsync(
        int performedByUserId,
        int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@UserId", userId, DbType.Int32);

        return await connection.QuerySingleAsync<int>(
            "dbo.usp_Users_Deactivate",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    private sealed class CreateUserResult
    {
        public int StatusCode { get; set; }
        public int? UserId { get; set; }
    }
    
public async Task<IReadOnlyList<UserListItem>> GetAllUsersAsync(
    int performedByUserId)
{
    var parameters = new DynamicParameters();
    parameters.Add(
        "@PerformedByUserId",
        performedByUserId,
        DbType.Int32);

    var users = await connection.QueryAsync<UserListItem>(
        "dbo.usp_Users_GetAll",
        parameters,
        commandType: CommandType.StoredProcedure);

    return users.AsList();
}

}
