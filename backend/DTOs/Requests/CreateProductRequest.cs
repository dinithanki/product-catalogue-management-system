
using System.ComponentModel.DataAnnotations;
using backend.Validation;

namespace backend.DTOs.Requests;

public sealed class CreateProductRequest
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    [Sku]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int SupplierId { get; set; }

    [Range(typeof(decimal), "0", "99999999999999.9999")]
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string PackSize { get; set; } = string.Empty;
}
