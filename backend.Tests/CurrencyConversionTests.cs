using backend.DTOs.External;
using backend.DTOs.Requests;
using backend.DTOs.Responses;
using backend.ExternalServices;
using backend.Repositories;
using backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace backend.Tests;

public sealed class CurrencyConversionTests
{
    [Fact]
    public async Task GetPagedAsync_ConvertsAndRoundsAwayFromZeroToTwoPlaces()
    {
        var product = Product("USD", 1.005m);
        var repository = new Mock<IProductRepository>();
        repository.Setup(x => x.GetPagedAsync(It.IsAny<ProductQueryRequest>()))
            .ReturnsAsync(((IReadOnlyList<ProductResponse>)new[] { product }, 1L));
        var rates = Rates(1m, 1m, 1m);
        var service = Service(repository, rates);

        var result = await service.GetPagedAsync(new ProductQueryRequest());

        Assert.Equal(1.01m, result.Items[0].LkrEquivalent);
        Assert.Equal(1m, result.Items[0].ExchangeRateApplied);
        Assert.Equal(DateTimeOffset.Parse("2026-10-23T00:00:01Z"), result.Items[0].ExchangeRateTimestamp);
    }

    [Fact]
    public async Task GetPagedAsync_UsesUppercaseTrimmedProductCurrency()
    {
        var product = Product(" gbp ", 3m);
        var repository = new Mock<IProductRepository>();
        repository.Setup(x => x.GetPagedAsync(It.IsAny<ProductQueryRequest>()))
            .ReturnsAsync(((IReadOnlyList<ProductResponse>)new[] { product }, 1L));
        var service = Service(repository, Rates(300m, 1.25m, 0m));

        var result = await service.GetPagedAsync(new ProductQueryRequest());

        Assert.Equal(240m, result.Items[0].ExchangeRateApplied);
        Assert.Equal(720m, result.Items[0].LkrEquivalent);
    }

    [Fact]
    public async Task CachedExchangeRateService_ReturnsCachedRatesWithoutProviderCall()
    {
        var provider = new Mock<IExchangeRateService>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var expected = Rates(300m, 1m, 1.2m);
        cache.Set("ExchangeRates.USD", expected, TimeSpan.FromMinutes(30));
        provider.Setup(x => x.GetLatestRatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        var actual = await new CachedExchangeRateService(provider.Object, cache).GetLatestRatesAsync();

        Assert.Same(expected, actual);
        provider.Verify(x => x.GetLatestRatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CachedExchangeRateService_RequestsFreshRatesAfterExpiredEntry()
    {
        var provider = new Mock<IExchangeRateService>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var oldRates = Rates(300m, 1m, 1.2m);
        var freshRates = Rates(310m, 1m, 1.2m);
        cache.Set("ExchangeRates.USD", oldRates, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(-1)
        });
        provider.Setup(x => x.GetLatestRatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(freshRates);

        var actual = await new CachedExchangeRateService(provider.Object, cache).GetLatestRatesAsync();

        Assert.Same(freshRates, actual);
        provider.Verify(x => x.GetLatestRatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CachedExchangeRateService_PropagatesProviderFailureWhenCacheIsExpired()
    {
        var provider = new Mock<IExchangeRateService>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("ExchangeRates.USD", Rates(300m, 1m, 1.2m), new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(-1)
        });
        provider.Setup(x => x.GetLatestRatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => new CachedExchangeRateService(provider.Object, cache).GetLatestRatesAsync());
    }

    [Fact]
    public async Task ExchangeRateService_RejectsIncompleteProviderResponse()
    {
        var client = new HttpClient(new StaticResponseHandler("""{"result":"success","base_code":"USD","conversion_rates":{}}"""))
        {
            BaseAddress = new Uri("https://rates.example/")
        };
        var service = new ExchangeRateService(client, new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExchangeRateApi:ApiKey"] = "test-key" })
            .Build());

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetLatestRatesAsync());
    }

    [Fact]
    public async Task ExchangeRateService_RejectsProviderErrorResult()
    {
        var client = new HttpClient(new StaticResponseHandler("""{"result":"error","base_code":"USD","conversion_rates":{"USD":1}}"""))
        {
            BaseAddress = new Uri("https://rates.example/")
        };
        var service = new ExchangeRateService(client, new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExchangeRateApi:ApiKey"] = "test-key" })
            .Build());

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetLatestRatesAsync());
    }

    private static ProductService Service(Mock<IProductRepository> repository, ExchangeRateApiResponse rates)
    {
        var provider = new Mock<IExchangeRateService>();
        provider.Setup(x => x.GetLatestRatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rates);
        return new ProductService(repository.Object,
            new CachedExchangeRateService(provider.Object, new MemoryCache(new MemoryCacheOptions())));
    }

    private static ProductResponse Product(string currency, decimal price) => new()
    {
        CurrencyCode = currency, UnitPrice = price
    };

    private static ExchangeRateApiResponse Rates(decimal lkr, decimal gbp, decimal usd) => new()
    {
        Result = "success", BaseCode = "USD", TimeLastUpdateUtc = "Fri, 23 Oct 2026 00:00:01 +0000",
        ConversionRates = new Dictionary<string, decimal> { ["LKR"] = lkr, ["GBP"] = gbp, ["USD"] = usd }
    };

    private sealed class StaticResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
