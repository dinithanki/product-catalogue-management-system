
using backend.DTOs.Responses;

namespace backend.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(
        string username,
        string password,
        Action<string, DateTime> setRefreshCookie);

    Task<bool> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword);

    Task<LoginResponse?> RefreshTokenAsync(
        string refreshToken,
        Action<string, DateTime> setRefreshCookie);

    Task<bool> LogoutAsync(
        string refreshToken);
}
