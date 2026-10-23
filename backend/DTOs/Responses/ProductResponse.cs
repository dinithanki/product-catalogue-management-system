
namespace backend.DTOs.Responses;

public sealed class ProductResponse
{
    public int ProductId { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public string PackSize { get; set; } = string.Empty;

    // Currency conversion details
    public decimal LkrEquivalent { get; set; }

    public decimal ExchangeRateApplied { get; set; }

    public DateTimeOffset ExchangeRateTimestamp { get; set; }
}
