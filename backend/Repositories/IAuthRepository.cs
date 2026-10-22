
using backend.Models;

namespace backend.Repositories;

public interface IAuthRepository
{
    Task<LoginUser?> GetUserByUsernameAsync(string username);

    Task<LoginUser?> GetUserByIdAsync(int userId);

    Task<int> CreateRefreshTokenAsync(
        int userId,
        string tokenHash,
        Guid tokenFamilyId,
        DateTime expiresAtUtc);

    Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(
        string currentTokenHash,
        string newTokenHash,
        DateTime newExpiresAtUtc);

    Task<int> RevokeRefreshTokenFamilyAsync(string tokenHash);

    Task<int> RevokeAllRefreshTokensForUserAsync(int userId);

    Task<int> ChangePasswordAsync(
        int userId,
        string newPasswordHash);
}

public sealed record RefreshTokenRotationResult(
    int StatusCode,
    int? UserId,
    Guid? TokenFamilyId);
