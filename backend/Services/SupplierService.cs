
using backend.DTOs.Requests;
using backend.DTOs.Responses;
using backend.Repositories;

namespace backend.Services;

public sealed class SupplierService(ISupplierRepository supplierRepository)
    : ISupplierService
{
    public async Task<IReadOnlyList<ActiveSupplierResponse>> GetActiveSuppliersAsync()
        => await supplierRepository.GetActiveSuppliersAsync();

    public async Task<IReadOnlyList<SupplierResponse>> GetAllSuppliersAsync()
        => await supplierRepository.GetAllSuppliersAsync();

    public async Task<(int StatusCode, int? SupplierId)> CreateSupplierAsync(
        int performedByUserId,
        CreateSupplierRequest request)
        => await supplierRepository.CreateSupplierAsync(performedByUserId, request);

    public async Task<int> UpdateSupplierAsync(
        int performedByUserId,
        int supplierId,
        UpdateSupplierRequest request)
        => await supplierRepository.UpdateSupplierAsync(
            performedByUserId,
            supplierId,
            request);

    public async Task<int> DeactivateSupplierAsync(
        int performedByUserId,
        int supplierId)
        => await supplierRepository.DeactivateSupplierAsync(
            performedByUserId,
            supplierId);
            
public async Task<int> ReactivateSupplierAsync(
    int performedByUserId,
    int supplierId)
    => await supplierRepository.ReactivateSupplierAsync(
        performedByUserId,
        supplierId);


    
}
