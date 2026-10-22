
namespace backend.Models;

public sealed class LoginUser
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; }

    public bool IsActive { get; set; }
}
