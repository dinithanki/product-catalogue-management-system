using System.ComponentModel.DataAnnotations;
using backend.DTOs.Requests;

namespace backend.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void CreateProductRequest_RequiresAllTextFields()
    {
        var request = new CreateProductRequest
        {
            SKU = "", Name = "", Category = "", SupplierId = 0,
            UnitPrice = -1m, PackSize = ""
        };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.SKU)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Name)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Category)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.SupplierId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.UnitPrice)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.PackSize)));
    }

    [Theory]
    [InlineData("A", 150, 100, 50, true)]
    [InlineData("A", 1, 1, 1, true)]
    [InlineData("A", 151, 100, 50, false)]
    [InlineData("A", 150, 101, 50, false)]
    [InlineData("A", 150, 100, 51, false)]
    [InlineData("", 150, 100, 50, false)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", 150, 100, 50, false)]
    public void CreateProductRequest_EnforcesStringBoundaries(
        string sku, int nameLength, int categoryLength, int packLength, bool valid)
    {
        var request = new CreateProductRequest
        {
            SKU = sku,
            Name = new string('N', nameLength),
            Category = new string('C', categoryLength),
            SupplierId = 1,
            UnitPrice = 0m,
            PackSize = new string('P', packLength)
        };

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("ABC-01_x", true)]
    [InlineData("a", true)]
    [InlineData("bad sku", false)]
    [InlineData("sku!", false)]
    [InlineData("", false)]
    public void CreateProductRequest_ValidatesSkuPattern(string sku, bool valid)
    {
        var request = ValidProduct();
        request.SKU = sku;

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("99999999999999.9999", true)]
    [InlineData("-0.01", false)]
    [InlineData("100000000000000", false)]
    public void CreateProductRequest_ValidatesUnitPriceRange(string price, bool valid)
    {
        var request = ValidProduct();
        request.UnitPrice = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("USD", true)]
    [InlineData("uSd", true)]
    [InlineData("US", false)]
    [InlineData("USDD", false)]
    [InlineData("U1D", false)]
    public void CreateSupplierRequest_ValidatesCurrencyCode(string code, bool valid)
    {
        var request = new CreateSupplierRequest
        {
            Name = "Supplier", Country = "Sri Lanka", CurrencyCode = code,
            ContactNumber = "+94112223333", Email = "supplier@example.com"
        };
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("supplier@example.com", true)]
    [InlineData("not-an-email", false)]
    [InlineData("", false)]
    public void CreateSupplierRequest_ValidatesEmail(string email, bool valid)
    {
        var request = new CreateSupplierRequest
        {
            Name = "Supplier", Country = "Sri Lanka", CurrencyCode = "LKR",
            ContactNumber = "+94112223333", Email = email
        };
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(150, 100, 30, 254, true)]
    [InlineData(151, 100, 30, 254, false)]
    [InlineData(150, 101, 30, 254, false)]
    [InlineData(150, 100, 31, 254, false)]
    [InlineData(150, 100, 30, 255, false)]
    public void CreateSupplierRequest_EnforcesMaximumLengths(
        int nameLength, int countryLength, int contactLength, int emailLength, bool valid)
    {
        var request = new CreateSupplierRequest
        {
            Name = new string('N', nameLength),
            Country = new string('C', countryLength),
            CurrencyCode = "LKR",
            ContactNumber = new string('1', contactLength),
            Email = $"a@{new string('x', emailLength - 6)}.com"
        };

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(51, 1, false)]
    [InlineData(1, 129, false)]
    [InlineData(0, 0, false)]
    public void LoginRequest_ValidatesRequiredFieldsAndLengths(
        int usernameLength, int passwordLength, bool valid)
    {
        var request = new LoginRequest
        {
            Username = new string('u', usernameLength),
            Password = new string('p', passwordLength)
        };
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(12, true)]
    [InlineData(11, false)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void ChangePasswordRequest_ValidatesNewPasswordLength(int length, bool valid)
    {
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "current",
            NewPassword = new string('p', length)
        };
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 100, true)]
    [InlineData(1, 101, false)]
    public void ProductQueryRequest_ValidatesPagination(int page, int size, bool valid)
    {
        var request = new ProductQueryRequest { PageNumber = page, PageSize = size };
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("price", "DESC", true)]
    [InlineData("supplier", "ASC", false)]
    [InlineData("name", "descending", false)]
    public void ProductQueryRequest_ValidatesSortOptions(string sortBy, string direction, bool valid)
    {
        var request = new ProductQueryRequest { SortBy = sortBy, SortDirection = direction };
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    private static CreateProductRequest ValidProduct() => new()
    {
        SKU = "SKU_1", Name = "Product", Category = "Food", SupplierId = 1,
        UnitPrice = 12.3456m, PackSize = "1 unit"
    };

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);
        return results;
    }
}
