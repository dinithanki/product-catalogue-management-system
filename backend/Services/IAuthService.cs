
using backend.DTOs.Response;

namespace backend.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(
        string username,
        string password);

    Task<bool> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword);

    Task<LoginResponse?> RefreshTokenAsync(
        string refreshToken);

    Task<bool> LogoutAsync(
        string refreshToken);
}
