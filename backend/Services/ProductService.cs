
using System.Globalization;
using backend.DTOs.Requests;
using backend.DTOs.Responses;
using backend.ExternalServices;
using backend.Repositories;

namespace backend.Services;

public sealed class ProductService(
    IProductRepository repository,
    CachedExchangeRateService exchangeRateService)
    : IProductService
{
    public async Task<(
        IReadOnlyList<ProductResponse> Items,
        long TotalCount)> GetPagedAsync(ProductQueryRequest request)
    {
        var result = await repository.GetPagedAsync(request);

        if (result.Items.Count == 0)
        {
            return result;
        }

        var rates = await exchangeRateService.GetLatestRatesAsync();

        ApplyCurrencyConversion(result.Items, rates);

        return result;
    }

    public async Task<ProductResponse?> GetByIdAsync(int productId)
    {
        var product = await repository.GetByIdAsync(productId);

        if (product is null)
        {
            return null;
        }

        var rates = await exchangeRateService.GetLatestRatesAsync();

        ApplyCurrencyConversion(
            new[] { product },
            rates);

        return product;
    }

    public Task<(int StatusCode, int? ProductId)> CreateAsync(
        int performedByUserId,
        CreateProductRequest request)
    {
        return repository.CreateAsync(performedByUserId, request);
    }

    public Task<int> UpdateAsync(
        int performedByUserId,
        int productId,
        UpdateProductRequest request)
    {
        return repository.UpdateAsync(
            performedByUserId,
            productId,
            request);
    }

    public Task<int> SoftDeleteAsync(
        int performedByUserId,
        int productId)
    {
        return repository.SoftDeleteAsync(
            performedByUserId,
            productId);
    }

    private static void ApplyCurrencyConversion(
        IEnumerable<ProductResponse> products,
        DTOs.External.ExchangeRateApiResponse rates)
    {
        if (!DateTimeOffset.TryParse(
                rates.TimeLastUpdateUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            throw new InvalidOperationException(
                "The exchange-rate API returned an invalid update timestamp.");
        }

        if (!rates.ConversionRates.TryGetValue(
                "LKR",
                out var lkrPerUsd) ||
            lkrPerUsd <= 0)
        {
            throw new InvalidOperationException(
                "A valid USD-to-LKR exchange rate is unavailable.");
        }

        foreach (var product in products)
        {
            var currency = product.CurrencyCode.Trim().ToUpperInvariant();

            if (!rates.ConversionRates.TryGetValue(
                    currency,
                    out var currencyPerUsd) ||
                currencyPerUsd <= 0)
            {
                throw new InvalidOperationException(
                    $"Exchange rate for currency '{currency}' is unavailable.");
            }

            var lkrPerCurrency = lkrPerUsd / currencyPerUsd;

            product.ExchangeRateApplied = lkrPerCurrency;
            product.LkrEquivalent = Math.Round(
                product.UnitPrice * lkrPerCurrency,
                2,
                MidpointRounding.AwayFromZero);

            product.ExchangeRateTimestamp = timestamp;
        }
    }
}
