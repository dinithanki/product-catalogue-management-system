
using System.Security.Claims;
using backend.Middleware;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace backend.Tests;

public sealed class PasswordChangeMiddlewareTests
{
    private static DefaultHttpContext CreateAuthenticatedContext(
        string path = "/api/products",
        string? userId = "1")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        var claims = new List<Claim>();

        if (userId is not null)
        {
            claims.Add(new Claim(
                ClaimTypes.NameIdentifier,
                userId));
        }

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(claims, "TestAuth"));

        context.Response.Body = new MemoryStream();

        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenPasswordChangeRequired_Returns403()
    {
        var repository = new Mock<IAuthRepository>();

        repository
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(new LoginUser
            {
                UserId = 1,
                IsActive = true,
                MustChangePassword = true
            });

        var nextCalled = false;

        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new PasswordChangeMiddleware(next);
        var context = CreateAuthenticatedContext();

        await middleware.InvokeAsync(context, repository.Object);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            context.Response.StatusCode);

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserInactive_Returns403()
    {
        var repository = new Mock<IAuthRepository>();

        repository
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(new LoginUser
            {
                UserId = 1,
                IsActive = false,
                MustChangePassword = false
            });

        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new PasswordChangeMiddleware(next);
        var context = CreateAuthenticatedContext();

        await middleware.InvokeAsync(context, repository.Object);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsActiveAndPasswordIsCurrent_Continues()
    {
        var repository = new Mock<IAuthRepository>();

        repository
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(new LoginUser
            {
                UserId = 1,
                IsActive = true,
                MustChangePassword = false
            });

        var nextCalled = false;

        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new PasswordChangeMiddleware(next);
        var context = CreateAuthenticatedContext();

        await middleware.InvokeAsync(context, repository.Object);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIdClaimIsInvalid_Returns403()
    {
        var repository = new Mock<IAuthRepository>();
        var nextCalled = false;

        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new PasswordChangeMiddleware(next);
        var context = CreateAuthenticatedContext(userId: "invalid");

        await middleware.InvokeAsync(context, repository.Object);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            context.Response.StatusCode);

        Assert.False(nextCalled);

        repository.Verify(
            r => r.GetUserByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/change-password")]
    public async Task InvokeAsync_WhenAllowedAuthEndpoint_SkipsAccountChecks(
        string path)
    {
        var repository = new Mock<IAuthRepository>();
        var nextCalled = false;

        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new PasswordChangeMiddleware(next);
        var context = CreateAuthenticatedContext(path);

        await middleware.InvokeAsync(context, repository.Object);

        Assert.True(nextCalled);

        repository.Verify(
            r => r.GetUserByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserDoesNotExist_Returns403()
    {
        var repository = new Mock<IAuthRepository>();

        repository
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync((LoginUser?)null);

        var middleware = new PasswordChangeMiddleware(
            _ => Task.CompletedTask);

        var context = CreateAuthenticatedContext();

        await middleware.InvokeAsync(context, repository.Object);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthenticated_ContinuesWithoutDatabaseCheck()
    {
        var repository = new Mock<IAuthRepository>();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/products";

        var nextCalled = false;

        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new PasswordChangeMiddleware(next);

        await middleware.InvokeAsync(context, repository.Object);

        Assert.True(nextCalled);

        repository.Verify(
            r => r.GetUserByIdAsync(It.IsAny<int>()),
            Times.Never);
    }
}
