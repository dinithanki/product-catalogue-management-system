
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace backend.Tests;

public sealed class UserServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IPasswordHasher<LoginUser>> _passwordHasher = new();

    private UserService CreateService() =>
        new(_repository.Object, _passwordHasher.Object);

    [Fact]
    public async Task CreateUserAsync_WithValidInput_GeneratesAndHashesPassword()
    {
        // Arrange
        _passwordHasher
            .Setup(x => x.HashPassword(
                It.IsAny<LoginUser>(),
                It.IsAny<string>()))
            .Returns("hashed-password");

        _repository
            .Setup(x => x.CreateUserAsync(
                1, "newuser", "hashed-password", "User"))
            .ReturnsAsync((0, (int?)10));

        var service = CreateService();

        // Act
        var (statusCode, userId, temporaryPassword) =
            await service.CreateUserAsync(1, "newuser", "User");

        // Assert
        Assert.Equal(0, statusCode);
        Assert.Equal(10, userId);
        Assert.NotNull(temporaryPassword);
        Assert.Equal(16, temporaryPassword.Length);

        _passwordHasher.Verify(
            x => x.HashPassword(
                It.IsAny<LoginUser>(),
                temporaryPassword),
            Times.Once);

        _repository.Verify(
            x => x.CreateUserAsync(
                1,
                "newuser",
                "hashed-password",
                "User"),
            Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidRole_ReturnsInvalidInput()
    {
        // Arrange
        var service = CreateService();

        // Act
        var (statusCode, userId, temporaryPassword) =
            await service.CreateUserAsync(1, "newuser", "SuperAdmin");

        // Assert
        Assert.Equal(1, statusCode);
        Assert.Null(userId);
        Assert.Null(temporaryPassword);

        _repository.Verify(
            x => x.CreateUserAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenRepositoryRejectsRequest_DoesNotReturnPassword()
    {
        // Arrange
        _passwordHasher
            .Setup(x => x.HashPassword(
                It.IsAny<LoginUser>(),
                It.IsAny<string>()))
            .Returns("hashed-password");

        _repository
            .Setup(x => x.CreateUserAsync(
                1, "newuser", "hashed-password", "User"))
            .ReturnsAsync((4, (int?)null));

        var service = CreateService();

        // Act
        var (statusCode, userId, temporaryPassword) =
            await service.CreateUserAsync(1, "newuser", "User");

        // Assert
        Assert.Equal(4, statusCode);
        Assert.Null(userId);
        Assert.Null(temporaryPassword);
    }

    [Fact]
    public async Task ResetUserPasswordAsync_GeneratesAndHashesPassword()
    {
        // Arrange
        _passwordHasher
            .Setup(x => x.HashPassword(
                It.IsAny<LoginUser>(),
                It.IsAny<string>()))
            .Returns("new-hashed-password");

        _repository
            .Setup(x => x.ResetUserPasswordAsync(
                1, 5, "new-hashed-password"))
            .ReturnsAsync(0);

        var service = CreateService();

        // Act
        var (statusCode, temporaryPassword) =
            await service.ResetUserPasswordAsync(1, 5);

        // Assert
        Assert.Equal(0, statusCode);
        Assert.NotNull(temporaryPassword);
        Assert.Equal(16, temporaryPassword.Length);

        _passwordHasher.Verify(
            x => x.HashPassword(
                It.Is<LoginUser>(u => u.UserId == 5),
                temporaryPassword),
            Times.Once);

        _repository.Verify(
            x => x.ResetUserPasswordAsync(
                1, 5, "new-hashed-password"),
            Times.Once);
    }

    [Fact]
    public async Task ResetUserPasswordAsync_CanGenerateDifferentPasswordsRepeatedly()
    {
        // Arrange
        _passwordHasher
            .Setup(x => x.HashPassword(
                It.IsAny<LoginUser>(),
                It.IsAny<string>()))
            .Returns("hashed-password");

        _repository
            .Setup(x => x.ResetUserPasswordAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(0);

        var service = CreateService();

        // Act
        var first = await service.ResetUserPasswordAsync(1, 5);
        var second = await service.ResetUserPasswordAsync(1, 5);

        // Assert
        Assert.Equal(0, first.StatusCode);
        Assert.Equal(0, second.StatusCode);
        Assert.NotNull(first.TemporaryPassword);
        Assert.NotNull(second.TemporaryPassword);
        Assert.NotEqual(first.TemporaryPassword, second.TemporaryPassword);
    }

    [Fact]
    public async Task ResetUserPasswordAsync_WithInvalidUserId_ReturnsInvalidInput()
    {
        // Arrange
        var service = CreateService();

        // Act
        var (statusCode, temporaryPassword) =
            await service.ResetUserPasswordAsync(1, 0);

        // Assert
        Assert.Equal(1, statusCode);
        Assert.Null(temporaryPassword);

        _repository.Verify(
            x => x.ResetUserPasswordAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetUserPasswordAsync_WhenUserNotFound_DoesNotReturnPassword()
    {
        // Arrange
        _passwordHasher
            .Setup(x => x.HashPassword(
                It.IsAny<LoginUser>(),
                It.IsAny<string>()))
            .Returns("hashed-password");

        _repository
            .Setup(x => x.ResetUserPasswordAsync(
                1, 5, "hashed-password"))
            .ReturnsAsync(2);

        var service = CreateService();

        // Act
        var (statusCode, temporaryPassword) =
            await service.ResetUserPasswordAsync(1, 5);

        // Assert
        Assert.Equal(2, statusCode);
        Assert.Null(temporaryPassword);
    }

    [Fact]
    public async Task UpdateUserAsync_WithValidInput_ReturnsRepositoryStatus()
    {
        // Arrange
        _repository
            .Setup(x => x.UpdateUserAsync(
                1, 5, "updateduser", "User"))
            .ReturnsAsync(0);

        var service = CreateService();

        // Act
        var statusCode = await service.UpdateUserAsync(
            1, 5, "updateduser", "User");

        // Assert
        Assert.Equal(0, statusCode);

        _repository.Verify(
            x => x.UpdateUserAsync(
                1, 5, "updateduser", "User"),
            Times.Once);
    }

    // TEST 1: Admin cannot change their own role.
    [Fact]
    public async Task UpdateUserAsync_WhenAdminChangesOwnRole_ReturnsError()
    {
        // Arrange
        var service = CreateService();

        // Act
        var statusCode = await service.UpdateUserAsync(
            performedByUserId: 1,
            userId: 1,
            username: "admin",
            role: "User");

        // Assert
        Assert.Equal(5, statusCode);

        _repository.Verify(
            x => x.UpdateUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    // TEST 2: Admin can update their own username.
    [Fact]
    public async Task UpdateUserAsync_WhenAdminUpdatesOwnUsername_Succeeds()
    {
        // Arrange
        _repository
            .Setup(x => x.UpdateUserAsync(
                1, 1, "updatedadmin", "Admin"))
            .ReturnsAsync(0);

        var service = CreateService();

        // Act
        var statusCode = await service.UpdateUserAsync(
            performedByUserId: 1,
            userId: 1,
            username: " updatedadmin ",
            role: "Admin");

        // Assert
        Assert.Equal(0, statusCode);

        _repository.Verify(
            x => x.UpdateUserAsync(
                1, 1, "updatedadmin", "Admin"),
            Times.Once);
    }

    // TEST 3: Admin can change another user's role.
    [Fact]
    public async Task UpdateUserAsync_WhenAdminChangesAnotherUsersRole_Succeeds()
    {
        // Arrange
        _repository
            .Setup(x => x.UpdateUserAsync(
                1, 5, "updateduser", "Admin"))
            .ReturnsAsync(0);

        var service = CreateService();

        // Act
        var statusCode = await service.UpdateUserAsync(
            performedByUserId: 1,
            userId: 5,
            username: "updateduser",
            role: "Admin");

        // Assert
        Assert.Equal(0, statusCode);

        _repository.Verify(
            x => x.UpdateUserAsync(
                1, 5, "updateduser", "Admin"),
            Times.Once);
    }

    // TEST 4: Invalid role is rejected.
    [Fact]
    public async Task UpdateUserAsync_WithInvalidRole_ReturnsInvalidInput()
    {
        // Arrange
        var service = CreateService();

        // Act
        var statusCode = await service.UpdateUserAsync(
            performedByUserId: 1,
            userId: 5,
            username: "updateduser",
            role: "SuperAdmin");

        // Assert
        Assert.Equal(1, statusCode);

        _repository.Verify(
            x => x.UpdateUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ActivateUserAsync_WithValidId_ReturnsRepositoryStatus()
    {
        // Arrange
        _repository
            .Setup(x => x.ActivateUserAsync(1, 5))
            .ReturnsAsync(0);

        var service = CreateService();

        // Act
        var statusCode = await service.ActivateUserAsync(1, 5);

        // Assert
        Assert.Equal(0, statusCode);
    }

    [Fact]
    public async Task DeactivateUserAsync_WithValidId_ReturnsRepositoryStatus()
    {
        // Arrange
        _repository
            .Setup(x => x.DeactivateUserAsync(1, 5))
            .ReturnsAsync(0);

        var service = CreateService();

        // Act
        var statusCode = await service.DeactivateUserAsync(1, 5);

        // Assert
        Assert.Equal(0, statusCode);
    }
}
