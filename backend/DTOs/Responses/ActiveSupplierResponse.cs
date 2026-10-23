
namespace backend.DTOs.Responses;

public sealed class ActiveSupplierResponse
{
    public int SupplierId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;
}
