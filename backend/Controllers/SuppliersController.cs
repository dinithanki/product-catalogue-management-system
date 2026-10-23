using backend.DTOs.Requests;
using System.Security.Claims;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public sealed class SuppliersController(ISupplierService supplierService)
    : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSuppliers()
    {
        var suppliers = await supplierService.GetActiveSuppliersAsync();

        return Ok(suppliers);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllSuppliers()
    {
        var suppliers = await supplierService.GetAllSuppliersAsync();

        return Ok(suppliers);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSupplier(
        [FromBody] CreateSupplierRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var result = await supplierService.CreateSupplierAsync(
            userId,
            request);

        return result.StatusCode switch
        {
            0 => CreatedAtAction(
                nameof(GetAllSuppliers),
                new { id = result.SupplierId },
                new
                {
                    supplierId = result.SupplierId,
                    message = "Supplier created successfully."
                }),

            1 => BadRequest(new
            {
                message = "Invalid supplier details."
            }),

            3 => Forbid(),

            _ => StatusCode(500, new
            {
                message = "An unexpected error occurred."
            })
        };
    }
    
[HttpPut("{id:int}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> UpdateSupplier(
    int id,
    [FromBody] UpdateSupplierRequest request)
{
    var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdValue, out var userId))
        return Unauthorized();

    var statusCode = await supplierService.UpdateSupplierAsync(
        userId,
        id,
        request);

    return statusCode switch
    {
        0 => Ok(new { message = "Supplier updated successfully." }),
        1 => BadRequest(new { message = "Invalid supplier details." }),
        2 => NotFound(new { message = "Supplier not found." }),
        3 => Forbid(),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}

[HttpDelete("{id:int}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeactivateSupplier(int id)
{
    var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdValue, out var userId))
        return Unauthorized();

    var statusCode = await supplierService.DeactivateSupplierAsync(
        userId,
        id);

    return statusCode switch
    {
        0 => Ok(new { message = "Supplier deactivated successfully." }),
        2 => NotFound(new { message = "Supplier not found." }),
        3 => Forbid(),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}

[HttpPatch("{id:int}/reactivate")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ReactivateSupplier(int id)
{
    var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (!int.TryParse(userIdValue, out var userId))
        return Unauthorized();

    var statusCode = await supplierService.ReactivateSupplierAsync(
        userId,
        id);

    return statusCode switch
    {
        0 => Ok(new { message = "Supplier reactivated successfully." }),
        2 => NotFound(new { message = "Supplier not found." }),
        3 => Forbid(),
        _ => StatusCode(500, new { message = "An unexpected error occurred." })
    };
}




}
