
using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Requests;

public sealed class ProductQueryRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [StringLength(100)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? SupplierId { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [RegularExpression(
        "^(name|sku|category|price)$",
        ErrorMessage = "SortBy must be name, sku, category, or price.")]
    public string SortBy { get; set; } = "name";

    [RegularExpression(
        "^(ASC|DESC)$",
        ErrorMessage = "SortDirection must be ASC or DESC.")]
    public string SortDirection { get; set; } = "ASC";
}
