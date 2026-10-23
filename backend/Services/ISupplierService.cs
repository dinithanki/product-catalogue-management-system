
using backend.DTOs.Requests;
using backend.DTOs.Responses;

namespace backend.Services;

public interface ISupplierService
{
    Task<IReadOnlyList<ActiveSupplierResponse>> GetActiveSuppliersAsync();

    Task<IReadOnlyList<SupplierResponse>> GetAllSuppliersAsync();

    Task<(int StatusCode, int? SupplierId)> CreateSupplierAsync(
        int performedByUserId,
        CreateSupplierRequest request);

    Task<int> UpdateSupplierAsync(
        int performedByUserId,
        int supplierId,
        UpdateSupplierRequest request);

    Task<int> DeactivateSupplierAsync(
        int performedByUserId,
        int supplierId);

    
    Task<int> ReactivateSupplierAsync(
        int performedByUserId,
        int supplierId);


}
