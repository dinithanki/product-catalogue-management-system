
using backend.Repositories;

namespace backend.Services;

public interface IUserService
{
    Task<(int StatusCode, int? UserId, string? TemporaryPassword)>
        CreateUserAsync(
            int performedByUserId,
            string username,
            string role);

    Task<int> UpdateUserAsync(
        int performedByUserId,
        int userId,
        string username,
        string role);

    Task<(int StatusCode, string? TemporaryPassword)>
        ResetUserPasswordAsync(
            int performedByUserId,
            int userId);

    Task<int> ActivateUserAsync(
        int performedByUserId,
        int userId);

    Task<int> DeactivateUserAsync(
        int performedByUserId,
        int userId);

    Task<IReadOnlyList<UserListItem>> GetAllUsersAsync(
        int performedByUserId);
}
