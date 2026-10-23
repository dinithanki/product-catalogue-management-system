
using System.Text.Json;
using backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace backend.Tests;

public sealed class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_ReturnsSafe500Response()
    {
        // Arrange
        RequestDelegate next = _ =>
            throw new InvalidOperationException(
                "Sensitive internal error details.");

        var logger = Mock.Of<ILogger<GlobalExceptionMiddleware>>();

        var middleware = new GlobalExceptionMiddleware(next, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            context.Response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            context.Response.ContentType);

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Contains("An unexpected error occurred.", responseBody);
        Assert.Contains("traceId", responseBody);
        Assert.DoesNotContain(
            "Sensitive internal error details.",
            responseBody);
    }
}
