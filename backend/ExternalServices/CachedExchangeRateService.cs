
using backend.DTOs.External;
using Microsoft.Extensions.Caching.Memory;

namespace backend.ExternalServices;

public sealed class CachedExchangeRateService(
    IExchangeRateService exchangeRateService,
    IMemoryCache cache)
{
    private const string CacheKey = "ExchangeRates.USD";

    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromMinutes(30);

    public async Task<ExchangeRateApiResponse> GetLatestRatesAsync(
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(
                CacheKey,
                out ExchangeRateApiResponse? cachedRates)
            && cachedRates is not null)
        {
            return cachedRates;
        }

        var rates = await exchangeRateService.GetLatestRatesAsync(
            cancellationToken);

        cache.Set(
            CacheKey,
            rates,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });

        return rates;
    }
}
