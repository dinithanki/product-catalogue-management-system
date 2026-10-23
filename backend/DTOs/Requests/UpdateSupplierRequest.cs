
using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Requests;

public sealed class UpdateSupplierRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z]{3}$",
        ErrorMessage = "Currency code must contain exactly three letters.")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string ContactNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;
}
