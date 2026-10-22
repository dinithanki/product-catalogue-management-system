
namespace backend.Repositories;

public interface IUserRepository
{
    Task<(int StatusCode, int? UserId)> CreateUserAsync(
        int performedByUserId,
        string username,
        string passwordHash,
        string role);

    Task<int> UpdateUserAsync(
        int performedByUserId,
        int userId,
        string username,
        string role);

    Task<int> ResetUserPasswordAsync(
        int performedByUserId,
        int userId,
        string newPasswordHash);

    Task<int> ActivateUserAsync(
        int performedByUserId,
        int userId);

    Task<int> DeactivateUserAsync(
        int performedByUserId,
        int userId);

    Task<IReadOnlyList<UserListItem>> GetAllUsersAsync(
        int performedByUserId);
}


public sealed record UserListItem(
    int UserId,
    string Username,
    string Role,
    bool MustChangePassword,
    bool IsActive);

