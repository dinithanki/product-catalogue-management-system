
using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Request;

public sealed class LoginRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;
}
