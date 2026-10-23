
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using backend.DTOs.Requests;
using backend.DTOs.Responses;

namespace backend.Repositories;

public sealed class SupplierRepository(SqlConnection connection)
    : ISupplierRepository
{
    public async Task<IReadOnlyList<ActiveSupplierResponse>>
        GetActiveSuppliersAsync()
    {
        var suppliers = await connection.QueryAsync<ActiveSupplierResponse>(
            "dbo.usp_Suppliers_GetActive",
            commandType: CommandType.StoredProcedure);

        return suppliers.AsList();
    }

    public async Task<IReadOnlyList<SupplierResponse>>
        GetAllSuppliersAsync()
    {
        var suppliers = await connection.QueryAsync<SupplierResponse>(
            "dbo.usp_Suppliers_GetAll",
            commandType: CommandType.StoredProcedure);

        return suppliers.AsList();
    }

    public async Task<(int StatusCode, int? SupplierId)> CreateSupplierAsync(
        int performedByUserId,
        CreateSupplierRequest request)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
        parameters.Add("@Name", request.Name.Trim(), DbType.String, size: 150);
        parameters.Add("@Country", request.Country.Trim(), DbType.String, size: 100);
        parameters.Add("@CurrencyCode", request.CurrencyCode.Trim().ToUpperInvariant(),
            DbType.AnsiStringFixedLength, size: 3);
        parameters.Add("@ContactNumber", request.ContactNumber.Trim(),
            DbType.String, size: 30);
        parameters.Add("@Email", request.Email.Trim(), DbType.String, size: 254);

        var result = await connection.QuerySingleAsync<CreateSupplierResult>(
            "dbo.usp_Suppliers_Create",
            parameters,
            commandType: CommandType.StoredProcedure);

        return (result.StatusCode, result.SupplierId);
    }
    
public async Task<int> UpdateSupplierAsync(
    int performedByUserId,
    int supplierId,
    UpdateSupplierRequest request)
{
    var parameters = new DynamicParameters();

    parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
    parameters.Add("@SupplierId", supplierId, DbType.Int32);
    parameters.Add("@Name", request.Name.Trim(), DbType.String, size: 150);
    parameters.Add("@Country", request.Country.Trim(), DbType.String, size: 100);
    parameters.Add(
        "@CurrencyCode",
        request.CurrencyCode.Trim().ToUpperInvariant(),
        DbType.AnsiStringFixedLength,
        size: 3);
    parameters.Add(
        "@ContactNumber",
        request.ContactNumber.Trim(),
        DbType.String,
        size: 30);
    parameters.Add("@Email", request.Email.Trim(), DbType.String, size: 254);

    var result = await connection.QuerySingleAsync<UpdateSupplierResult>(
        "dbo.usp_Suppliers_Update",
        parameters,
        commandType: CommandType.StoredProcedure);

    return result.StatusCode;
}

public async Task<int> DeactivateSupplierAsync(
    int performedByUserId,
    int supplierId)
{
    var parameters = new DynamicParameters();

    parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
    parameters.Add("@SupplierId", supplierId, DbType.Int32);

    var result = await connection.QuerySingleAsync<DeactivateSupplierResult>(
        "dbo.usp_Suppliers_Deactivate",
        parameters,
        commandType: CommandType.StoredProcedure);

    return result.StatusCode;
}

private sealed class DeactivateSupplierResult
{
    public int StatusCode { get; set; }
}

private sealed class UpdateSupplierResult
{
    public int StatusCode { get; set; }
}


    private sealed class CreateSupplierResult
    {
        public int StatusCode { get; set; }

        public int? SupplierId { get; set; }
    }


public async Task<int> ReactivateSupplierAsync(
    int performedByUserId,
    int supplierId)
{
    var parameters = new DynamicParameters();

    parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
    parameters.Add("@SupplierId", supplierId, DbType.Int32);

    var result = await connection.QuerySingleAsync<ReactivateSupplierResult>(
        "dbo.usp_Suppliers_Reactivate",
        parameters,
        commandType: CommandType.StoredProcedure);

    return result.StatusCode;
}

private sealed class ReactivateSupplierResult
{
    public int StatusCode { get; set; }
}

}
