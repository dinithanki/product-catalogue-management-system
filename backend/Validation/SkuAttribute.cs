
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace backend.Validation;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = false)]
public sealed class SkuAttribute : ValidationAttribute
{
    private const string Pattern = @"^[A-Za-z0-9_-]+$";

    public SkuAttribute()
    {
        ErrorMessage =
            "SKU may contain only letters, numbers, hyphens, and underscores.";
    }

    public override bool IsValid(object? value)
    {
        // Required validation remains the responsibility of [Required].
        if (value is null)
        {
            return true;
        }

        if (value is not string sku)
        {
            return false;
        }

        return Regex.IsMatch(
            sku,
            Pattern,
            RegexOptions.CultureInvariant);
    }
}
