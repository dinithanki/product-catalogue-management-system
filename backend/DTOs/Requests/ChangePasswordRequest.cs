
using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Requests;

public sealed class ChangePasswordRequest
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string NewPassword { get; init; } = string.Empty;
}
