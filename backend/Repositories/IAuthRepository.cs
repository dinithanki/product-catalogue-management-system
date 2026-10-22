
using backend.Models;

namespace backend.Repositories;

public interface IAuthRepository
{
    Task<LoginUser?> GetUserByUsernameAsync(string username);

    Task<int> CreateRefreshTokenAsync(
        int userId,
        string tokenHash,
        Guid tokenFamilyId,
        DateTime expiresAtUtc);

    Task<LoginUser?> GetUserByIdAsync(int userId);

    Task<int> ChangePasswordAsync(
        int userId,
        string newPasswordHash);
}
