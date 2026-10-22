
using System.Data;
using Dapper;
using backend.Models;
using Microsoft.Data.SqlClient;

namespace backend.Repositories;

public sealed class AuthRepository(SqlConnection connection) : IAuthRepository
{
    public async Task<LoginUser?> GetUserByUsernameAsync(string username)
    {
        var parameters = new DynamicParameters();

        parameters.Add(
            "@Username",
            username,
            DbType.String,
            size: 50);

        return await connection.QuerySingleOrDefaultAsync<LoginUser>(
            "dbo.usp_Users_GetByUsername",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> CreateRefreshTokenAsync(
        int userId,
        string tokenHash,
        Guid tokenFamilyId,
        DateTime expiresAtUtc)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@UserId", userId, DbType.Int32);
        parameters.Add(
            "@TokenHash",
            tokenHash,
            DbType.AnsiStringFixedLength,
            size: 64);
        parameters.Add(
            "@TokenFamilyId",
            tokenFamilyId,
            DbType.Guid);
        parameters.Add(
            "@ExpiresAt",
            expiresAtUtc,
            DbType.DateTime2);

        return await connection.QuerySingleAsync<int>(
            "dbo.usp_RefreshTokens_Create",
            parameters,
            commandType: CommandType.StoredProcedure);
    }
    
public async Task<int> BootstrapFirstAdminAsync(
    string username,
    string passwordHash)
{
    var parameters = new DynamicParameters();

    parameters.Add(
        "@Username",
        username,
        DbType.String,
        size: 50);

    parameters.Add(
        "@PasswordHash",
        passwordHash,
        DbType.String,
        size: 255);

    return await connection.QuerySingleAsync<int>(
        "dbo.usp_Users_BootstrapFirstAdmin",
        parameters,
        commandType: CommandType.StoredProcedure);
}

public async Task<int> ChangePasswordAsync(int userId, string newPasswordHash)
{
    var parameters = new DynamicParameters();

    parameters.Add("@UserId", userId, DbType.Int32);
    parameters.Add("@NewPasswordHash", newPasswordHash, DbType.String, size: 255);

    return await connection.QuerySingleAsync<int>(
        "dbo.usp_Users_ChangePassword",
        parameters,
        commandType: CommandType.StoredProcedure);
}

public async Task<LoginUser?> GetUserByIdAsync(int userId)
{
    var parameters = new DynamicParameters();

    parameters.Add("@UserId", userId, DbType.Int32);

    return await connection.QuerySingleOrDefaultAsync<LoginUser>(
        "dbo.usp_Users_GetById",
        parameters,
        commandType: CommandType.StoredProcedure);
}

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

private sealed class CreateUserResult
{
    public int StatusCode { get; set; }
    public int? UserId { get; set; }
}


}
