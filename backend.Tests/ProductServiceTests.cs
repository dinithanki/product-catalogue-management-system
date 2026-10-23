
using backend.DTOs.External;
using backend.DTOs.Requests;
using backend.DTOs.Responses;
using backend.ExternalServices;
using backend.Repositories;
using backend.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace backend.Tests;

public sealed class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repository = new();

    private readonly Mock<IExchangeRateService> _exchangeRateService;

    private readonly CachedExchangeRateService _cachedExchangeRateService;

    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _exchangeRateService = new Mock<IExchangeRateService>();

        _cachedExchangeRateService = new CachedExchangeRateService(
            _exchangeRateService.Object,
            new MemoryCache(new MemoryCacheOptions()));

        _service = new ProductService(
            _repository.Object,
            _cachedExchangeRateService);
    }

    private static ExchangeRateApiResponse CreateRates()
    {
        return new ExchangeRateApiResponse
        {
            Result = "success",
            BaseCode = "USD",
            TimeLastUpdateUtc = "Fri, 23 Oct 2026 00:00:01 +0000",
            ConversionRates = new Dictionary<string, decimal>
            {
                ["USD"] = 1m,
                ["LKR"] = 300m,
                ["GBP"] = 1.25m
            }
        };
    }

    private static ProductResponse CreateProduct(
        string currency = "GBP",
        decimal unitPrice = 10m)
    {
        return new ProductResponse
        {
            ProductId = 1,
            SKU = "TEST-001",
            Name = "Test Product",
            Category = "Food",
            SupplierId = 1,
            SupplierName = "Test Supplier",
            CurrencyCode = currency,
            UnitPrice = unitPrice,
            PackSize = "1 pack"
        };
    }

    private void SetupExchangeRates(ExchangeRateApiResponse rates)
    {
        _exchangeRateService
            .Setup(service => service.GetLatestRatesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);
    }

    [Fact]
    public async Task GetPagedAsync_WhenNoProducts_ReturnsEmptyResultWithoutRates()
    {
        _repository
            .Setup(repository => repository.GetPagedAsync(
                It.IsAny<ProductQueryRequest>()))
            .ReturnsAsync((
                (IReadOnlyList<ProductResponse>)Array.Empty<ProductResponse>(),
                0L));

        var result = await _service.GetPagedAsync(new ProductQueryRequest());

        Assert.Empty(result.Items);
        Assert.Equal(0L, result.TotalCount);

        _exchangeRateService.Verify(
            service => service.GetLatestRatesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPagedAsync_WhenProductsExist_ConvertsPriceToLkr()
    {
        var product = CreateProduct("GBP", 10m);

        _repository
            .Setup(repository => repository.GetPagedAsync(
                It.IsAny<ProductQueryRequest>()))
            .ReturnsAsync((
                (IReadOnlyList<ProductResponse>)new[] { product },
                1L));

        SetupExchangeRates(CreateRates());

        var result = await _service.GetPagedAsync(new ProductQueryRequest());

        Assert.Single(result.Items);
        Assert.Equal(1L, result.TotalCount);

        // 1 GBP = 300 / 1.25 = 240 LKR.
        Assert.Equal(240m, result.Items[0].ExchangeRateApplied);
        Assert.Equal(2400m, result.Items[0].LkrEquivalent);
    }

    [Fact]
    public async Task GetPagedAsync_PassesQueryToRepository()
    {
        var request = new ProductQueryRequest
        {
            PageNumber = 2,
            PageSize = 5,
            Search = "Rice",
            SortBy = "name",
            SortDirection = "ASC"
        };

        _repository
            .Setup(repository => repository.GetPagedAsync(request))
            .ReturnsAsync((
                (IReadOnlyList<ProductResponse>)Array.Empty<ProductResponse>(),
                0L));

        await _service.GetPagedAsync(request);

        _repository.Verify(
            repository => repository.GetPagedAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(999))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);

        _exchangeRateService.Verify(
            service => service.GetLatestRatesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ConvertsPrice()
    {
        var product = CreateProduct("GBP", 5m);

        _repository
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        SetupExchangeRates(CreateRates());

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1200m, result.LkrEquivalent);
        Assert.Equal(240m, result.ExchangeRateApplied);
    }

    [Fact]
    public async Task GetPagedAsync_WhenLkrRateIsMissing_ThrowsInvalidOperationException()
    {
        var product = CreateProduct();

        _repository
            .Setup(repository => repository.GetPagedAsync(
                It.IsAny<ProductQueryRequest>()))
            .ReturnsAsync((
                (IReadOnlyList<ProductResponse>)new[] { product },
                1L));

        var rates = CreateRates();
        rates.ConversionRates.Remove("LKR");
        SetupExchangeRates(rates);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetPagedAsync(new ProductQueryRequest()));
    }

    [Fact]
    public async Task GetPagedAsync_WhenProductCurrencyIsMissing_ThrowsInvalidOperationException()
    {
        var product = CreateProduct("EUR");

        _repository
            .Setup(repository => repository.GetPagedAsync(
                It.IsAny<ProductQueryRequest>()))
            .ReturnsAsync((
                (IReadOnlyList<ProductResponse>)new[] { product },
                1L));

        SetupExchangeRates(CreateRates());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetPagedAsync(new ProductQueryRequest()));
    }

    [Fact]
    public async Task CreateAsync_DelegatesToRepository()
    {
        var request = new CreateProductRequest
        {
            SKU = "TEST-001",
            Name = "Test Product",
            Category = "Food",
            SupplierId = 1,
            UnitPrice = 10m,
            PackSize = "1 pack"
        };

        _repository
            .Setup(repository => repository.CreateAsync(7, request))
            .ReturnsAsync((0, (int?)42));

        var result = await _service.CreateAsync(7, request);

        Assert.Equal(0, result.StatusCode);
        Assert.Equal(42, result.ProductId);
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToRepository()
    {
        var request = new UpdateProductRequest
        {
            SKU = "TEST-001",
            Name = "Updated Product",
            Category = "Food",
            SupplierId = 1,
            UnitPrice = 12m,
            PackSize = "2 packs"
        };

        _repository
            .Setup(repository => repository.UpdateAsync(7, 42, request))
            .ReturnsAsync(0);

        var result = await _service.UpdateAsync(7, 42, request);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task SoftDeleteAsync_DelegatesToRepository()
    {
        _repository
            .Setup(repository => repository.SoftDeleteAsync(7, 42))
            .ReturnsAsync(0);

        var result = await _service.SoftDeleteAsync(7, 42);

        Assert.Equal(0, result);
    }
}
