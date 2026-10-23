
using backend.DTOs.Requests;
using backend.DTOs.Responses;
using backend.Repositories;
using backend.Services;
using Moq;
using Xunit;

namespace backend.Tests;

public class SupplierServiceTests
{
    private readonly Mock<ISupplierRepository> _repositoryMock;
    private readonly SupplierService _supplierService;

    public SupplierServiceTests()
    {
        _repositoryMock = new Mock<ISupplierRepository>();
        _supplierService = new SupplierService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetActiveSuppliersAsync_ReturnsActiveSuppliers()
    {
        // Arrange
        IReadOnlyList<ActiveSupplierResponse> suppliers =
        [
            new ActiveSupplierResponse
            {
                SupplierId = 1,
                Name = "European Foods Ltd",
                Country = "United Kingdom",
                CurrencyCode = "GBP"
            }
        ];

        _repositoryMock
            .Setup(r => r.GetActiveSuppliersAsync())
            .ReturnsAsync(suppliers);

        // Act
        var result = await _supplierService.GetActiveSuppliersAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].SupplierId);
        Assert.Equal("European Foods Ltd", result[0].Name);
    }

    [Fact]
    public async Task GetAllSuppliersAsync_ReturnsAllSuppliers()
    {
        // Arrange
        IReadOnlyList<SupplierResponse> suppliers =
        [
            new SupplierResponse
            {
                SupplierId = 2,
                Name = "Pacific Electronics",
                Country = "China",
                CurrencyCode = "CNY",
                ContactNumber = "+86-13800138000",
                Email = "electronics@example.com",
                IsActive = false
            }
        ];

        _repositoryMock
            .Setup(r => r.GetAllSuppliersAsync())
            .ReturnsAsync(suppliers);

        // Act
        var result = await _supplierService.GetAllSuppliersAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].SupplierId);
        Assert.False(result[0].IsActive);
    }

    [Fact]
    public async Task CreateSupplierAsync_PassesRequestToRepository()
    {
        // Arrange
        var request = new CreateSupplierRequest
        {
            Name = "New Supplier",
            Country = "India",
            CurrencyCode = "INR",
            ContactNumber = "0771234567",
            Email = "new@example.com"
        };

        _repositoryMock
            .Setup(r => r.CreateSupplierAsync(1, request))
            .ReturnsAsync((0, (int?)5));

        // Act
        var result = await _supplierService.CreateSupplierAsync(1, request);

        // Assert
        Assert.Equal(0, result.StatusCode);
        Assert.Equal(5, result.SupplierId);
        _repositoryMock.Verify(
            r => r.CreateSupplierAsync(1, request),
            Times.Once);
    }

    [Fact]
    public async Task UpdateSupplierAsync_ReturnsRepositoryStatusCode()
    {
        // Arrange
        var request = new UpdateSupplierRequest
        {
            Name = "Updated Supplier",
            Country = "India",
            CurrencyCode = "INR",
            ContactNumber = "0771234567",
            Email = "updated@example.com"
        };

        _repositoryMock
            .Setup(r => r.UpdateSupplierAsync(1, 5, request))
            .ReturnsAsync(0);

        // Act
        var result = await _supplierService.UpdateSupplierAsync(1, 5, request);

        // Assert
        Assert.Equal(0, result);
        _repositoryMock.Verify(
            r => r.UpdateSupplierAsync(1, 5, request),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateSupplierAsync_ReturnsRepositoryStatusCode()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeactivateSupplierAsync(1, 5))
            .ReturnsAsync(0);

        // Act
        var result = await _supplierService.DeactivateSupplierAsync(1, 5);

        // Assert
        Assert.Equal(0, result);
        _repositoryMock.Verify(
            r => r.DeactivateSupplierAsync(1, 5),
            Times.Once);
    }

    [Fact]
    public async Task ReactivateSupplierAsync_ReturnsRepositoryStatusCode()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ReactivateSupplierAsync(1, 5))
            .ReturnsAsync(0);

        // Act
        var result = await _supplierService.ReactivateSupplierAsync(1, 5);

        // Assert
        Assert.Equal(0, result);
        _repositoryMock.Verify(
            r => r.ReactivateSupplierAsync(1, 5),
            Times.Once);
    }
}
