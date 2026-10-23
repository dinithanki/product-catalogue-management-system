
using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Requests;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
