using System.Security.Claims;
using backend.Controllers;
using backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace backend.Tests;

public sealed class ConflictResponseTests
{
    [Fact]
    public async Task UpdateUser_WhenRepositoryReportsDuplicateUsername_ReturnsConflict()
    {
        var service = new Mock<IUserService>();
        service
            .Setup(x => x.UpdateUserAsync(7, 12, "existing", "User"))
            .ReturnsAsync(6);

        var controller = new UsersController(service.Object)
        {
            ControllerContext = CreateControllerContext(7)
        };

        var result = await controller.UpdateUser(
            12,
            new UpdateUserRequest("existing", "User"));

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WhenRepositoryReportsDuplicateSku_ReturnsConflict()
    {
        var service = new Mock<IProductService>();
        service
            .Setup(x => x.CreateAsync(7, It.IsAny<backend.DTOs.Requests.CreateProductRequest>()))
            .ReturnsAsync((5, (int?)null));

        var controller = new ProductsController(service.Object)
        {
            ControllerContext = CreateControllerContext(7)
        };

        var result = await controller.Create(
            new backend.DTOs.Requests.CreateProductRequest());

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    private static ControllerContext CreateControllerContext(int userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Test");

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
