
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using backend.DTOs.Requests;
using backend.DTOs.Responses;

namespace backend.Repositories;

public sealed class ProductRepository(SqlConnection connection)
    : IProductRepository
{
    public async Task<(IReadOnlyList<ProductResponse> Items, long TotalCount)>
        GetPagedAsync(ProductQueryRequest request)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PageNumber", request.PageNumber, DbType.Int32);
        parameters.Add("@PageSize", request.PageSize, DbType.Int32);
        parameters.Add("@Search", request.Search, DbType.String, size: 100);
        parameters.Add("@SupplierId", request.SupplierId, DbType.Int32);
        parameters.Add("@Category", request.Category, DbType.String, size: 100);
        parameters.Add("@SortBy", request.SortBy, DbType.String, size: 20);
        parameters.Add("@SortDirection", request.SortDirection, DbType.String, size: 4);

        using var results = await connection.QueryMultipleAsync(
            "dbo.usp_Products_GetPaged",
            parameters,
            commandType: CommandType.StoredProcedure);

        var totalCount = await results.ReadSingleAsync<long>();
        var products = (await results.ReadAsync<ProductResponse>()).AsList();

        return (products, totalCount);
    }

    public async Task<ProductResponse?> GetByIdAsync(int productId)
    {
        return await connection.QuerySingleOrDefaultAsync<ProductResponse>(
            "dbo.usp_Products_GetById",
            new { ProductId = productId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<(int StatusCode, int? ProductId)> CreateAsync(
        int performedByUserId,
        CreateProductRequest request)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@SKU", request.SKU.Trim(), DbType.String, size: 50);
        parameters.Add("@Name", request.Name.Trim(), DbType.String, size: 150);
        parameters.Add("@Category", request.Category.Trim(), DbType.String, size: 100);
        parameters.Add("@SupplierId", request.SupplierId, DbType.Int32);
        parameters.Add(
            "@UnitPrice",
            request.UnitPrice,
            DbType.Decimal,
            precision: 18,
            scale: 4);
        parameters.Add("@PackSize", request.PackSize.Trim(), DbType.String, size: 50);

        var result = await connection.QuerySingleAsync<CreateProductResult>(
            "dbo.usp_Products_Create",
            parameters,
            commandType: CommandType.StoredProcedure);

        return (result.StatusCode, result.ProductId);
    }

    public async Task<int> UpdateAsync(
        int performedByUserId,
        int productId,
        UpdateProductRequest request)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@ProductId", productId, DbType.Int32);
        parameters.Add("@SKU", request.SKU.Trim(), DbType.String, size: 50);
        parameters.Add("@Name", request.Name.Trim(), DbType.String, size: 150);
        parameters.Add("@Category", request.Category.Trim(), DbType.String, size: 100);
        parameters.Add("@SupplierId", request.SupplierId, DbType.Int32);
        parameters.Add(
            "@UnitPrice",
            request.UnitPrice,
            DbType.Decimal,
            precision: 18,
            scale: 4);
        parameters.Add("@PackSize", request.PackSize.Trim(), DbType.String, size: 50);

        return await connection.QuerySingleAsync<int>(
            "dbo.usp_Products_Update",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> SoftDeleteAsync(
        int performedByUserId,
        int productId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@ProductId", productId, DbType.Int32);

        return await connection.QuerySingleAsync<int>(
            "dbo.usp_Products_SoftDelete",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    private sealed class CreateProductResult
    {
        public int StatusCode { get; set; }

        public int? ProductId { get; set; }
    }
}
