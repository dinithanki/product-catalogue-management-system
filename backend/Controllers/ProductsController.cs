
using System.Security.Claims;
using backend.DTOs.Requests;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(IProductService productService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] ProductQueryRequest request)
    {
        var (items, totalCount) =
            await productService.GetPagedAsync(request);

        return Ok(new
        {
            items,
            totalCount,
            pageNumber = request.PageNumber,
            pageSize = request.PageSize,
            totalPages = (int)Math.Ceiling(
                totalCount / (double)request.PageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid product ID." });
        }

        var product = await productService.GetByIdAsync(id);

        if (product is null)
        {
            return NotFound(new { message = "Product not found." });
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var (statusCode, productId) =
            await productService.CreateAsync(userId.Value, request);

        return statusCode switch
        {
            0 => CreatedAtAction(
                nameof(GetById),
                new { id = productId },
                new { productId, message = "Product created successfully." }),

            1 => BadRequest(new { message = "Invalid product data." }),

            3 => Forbid(),

            4 => BadRequest(new
            {
                message = "The selected supplier does not exist or is inactive."
            }),

            5 => Conflict(new { message = "SKU already exists." }),

            _ => StatusCode(500, new { message = "Unable to create product." })
        };
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid product ID." });
        }

        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var statusCode =
            await productService.UpdateAsync(userId.Value, id, request);

        return statusCode switch
        {
            0 => Ok(new { message = "Product updated successfully." }),

            1 => BadRequest(new { message = "Invalid product data." }),

            2 => NotFound(new { message = "Product not found." }),

            3 => Forbid(),

            4 => BadRequest(new
            {
                message = "The selected supplier does not exist or is inactive."
            }),

            5 => Conflict(new { message = "SKU already exists." }),

            _ => StatusCode(500, new { message = "Unable to update product." })
        };
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid product ID." });
        }

        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var statusCode =
            await productService.SoftDeleteAsync(userId.Value, id);

        return statusCode switch
        {
            0 => NoContent(),

            2 => NotFound(new { message = "Product not found." }),

            3 => Forbid(),

            _ => StatusCode(500, new { message = "Unable to delete product." })
        };
    }

    private int? GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }
}
