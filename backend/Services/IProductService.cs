
using backend.DTOs.Requests;
using backend.DTOs.Responses;

namespace backend.Services;

public interface IProductService
{
    Task<(IReadOnlyList<ProductResponse> Items, long TotalCount)>
        GetPagedAsync(ProductQueryRequest request);

    Task<ProductResponse?> GetByIdAsync(int productId);

    Task<(int StatusCode, int? ProductId)> CreateAsync(
        int performedByUserId,
        CreateProductRequest request);

    Task<int> UpdateAsync(
        int performedByUserId,
        int productId,
        UpdateProductRequest request);

    Task<int> SoftDeleteAsync(
        int performedByUserId,
        int productId);
}
