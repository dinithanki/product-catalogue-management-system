
using backend.DTOs.Response;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace backend.Tests;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _repositoryMock = new();
    private readonly Mock<IPasswordHasher<LoginUser>> _passwordHasherMock = new();
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "this-is-a-test-secret-key-at-least-32-characters-long",
            ["Jwt:Issuer"] = "KeellsTest",
            ["Jwt:Audience"] = "KeellsTestUsers"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        _authService = new AuthService(
            _repositoryMock.Object,
            _configuration,
            _passwordHasherMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponse()
    {
        // Arrange: create a test user.
        var user = new LoginUser
        {
            UserId = 1,
            Username = "testuser",
            PasswordHash = "fake-password-hash",
            Role = "User",
            IsActive = true,
            MustChangePassword = false
        };

        _repositoryMock
            .Setup(r => r.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(
                user,
                user.PasswordHash,
                "CorrectPassword123!"))
            .Returns(PasswordVerificationResult.Success);

        _repositoryMock
            .Setup(r => r.CreateRefreshTokenAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        // Act: call the actual login service.
        LoginResponse? result = await _authService.LoginAsync(
            "testuser",
            "CorrectPassword123!");

        // Assert: verify the expected result.
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal(1, result.UserId);
        Assert.Equal("User", result.Role);
        Assert.False(result.MustChangePassword);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }
    
[Fact]
public async Task LoginAsync_WithIncorrectPassword_ReturnsNull()
{
    // Arrange
    var user = new LoginUser
    {
        UserId = 1,
        Username = "testuser",
        PasswordHash = "fake-password-hash",
        Role = "User",
        IsActive = true,
        MustChangePassword = false
    };

    _repositoryMock
        .Setup(r => r.GetUserByUsernameAsync("testuser"))
        .ReturnsAsync(user);

    _passwordHasherMock
        .Setup(h => h.VerifyHashedPassword(
            user,
            user.PasswordHash,
            "WrongPassword123!"))
        .Returns(PasswordVerificationResult.Failed);

    // Act
    var result = await _authService.LoginAsync(
        "testuser",
        "WrongPassword123!");

    // Assert
    Assert.Null(result);

    _repositoryMock.Verify(
        r => r.CreateRefreshTokenAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<DateTime>()),
        Times.Never);
}

[Fact]
public async Task LoginAsync_WithInactiveUser_ReturnsNull()
{
    // Arrange
    var user = new LoginUser
    {
        UserId = 2,
        Username = "inactiveuser",
        PasswordHash = "fake-password-hash",
        Role = "User",
        IsActive = false,
        MustChangePassword = false
    };

    _repositoryMock
        .Setup(r => r.GetUserByUsernameAsync("inactiveuser"))
        .ReturnsAsync(user);

    // Act
    var result = await _authService.LoginAsync(
        "inactiveuser",
        "CorrectPassword123!");

    // Assert
    Assert.Null(result);

    _passwordHasherMock.Verify(
        h => h.VerifyHashedPassword(
            It.IsAny<LoginUser>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Never);
}

[Fact]
public async Task ChangePasswordAsync_WithCorrectCurrentPassword_ReturnsTrue()
{
    // Arrange
    var user = new LoginUser
    {
        UserId = 1,
        Username = "testuser",
        PasswordHash = "old-hash",
        Role = "User",
        IsActive = true,
        MustChangePassword = true
    };

    _repositoryMock
        .Setup(r => r.GetUserByIdAsync(1))
        .ReturnsAsync(user);

    _passwordHasherMock
        .Setup(h => h.VerifyHashedPassword(
            user, "old-hash", "OldPassword123!"))
        .Returns(PasswordVerificationResult.Success);

    _passwordHasherMock
        .Setup(h => h.HashPassword(user, "NewPassword123!"))
        .Returns("new-hash");

    _repositoryMock
        .Setup(r => r.ChangePasswordAsync(1, "new-hash"))
        .ReturnsAsync(1);

    // Act
    var result = await _authService.ChangePasswordAsync(
        1, "OldPassword123!", "NewPassword123!");

    // Assert
    Assert.True(result);

    _passwordHasherMock.Verify(
        h => h.HashPassword(user, "NewPassword123!"),
        Times.Once);

    _repositoryMock.Verify(
        r => r.ChangePasswordAsync(1, "new-hash"),
        Times.Once);
}

[Fact]
public async Task ChangePasswordAsync_WithIncorrectCurrentPassword_ReturnsFalse()
{
    // Arrange
    var user = new LoginUser
    {
        UserId = 1,
        Username = "testuser",
        PasswordHash = "old-hash",
        Role = "User",
        IsActive = true
    };

    _repositoryMock
        .Setup(r => r.GetUserByIdAsync(1))
        .ReturnsAsync(user);

    _passwordHasherMock
        .Setup(h => h.VerifyHashedPassword(
            user, "old-hash", "WrongPassword123!"))
        .Returns(PasswordVerificationResult.Failed);

    // Act
    var result = await _authService.ChangePasswordAsync(
        1, "WrongPassword123!", "NewPassword123!");

    // Assert
    Assert.False(result);

    _passwordHasherMock.Verify(
        h => h.HashPassword(
            It.IsAny<LoginUser>(),
            It.IsAny<string>()),
        Times.Never);

    _repositoryMock.Verify(
        r => r.ChangePasswordAsync(
            It.IsAny<int>(),
            It.IsAny<string>()),
        Times.Never);
}

[Fact]
public async Task ChangePasswordAsync_WithInactiveUser_ReturnsFalse()
{
    // Arrange
    var user = new LoginUser
    {
        UserId = 1,
        Username = "testuser",
        PasswordHash = "old-hash",
        Role = "User",
        IsActive = false
    };

    _repositoryMock
        .Setup(r => r.GetUserByIdAsync(1))
        .ReturnsAsync(user);

    // Act
    var result = await _authService.ChangePasswordAsync(
        1, "OldPassword123!", "NewPassword123!");

    // Assert
    Assert.False(result);

    _passwordHasherMock.Verify(
        h => h.VerifyHashedPassword(
            It.IsAny<LoginUser>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Never);
}

[Fact]
public async Task ChangePasswordAsync_WithSamePassword_ThrowsArgumentException()
{
    // Arrange
    var user = new LoginUser
    {
        UserId = 1,
        Username = "testuser",
        PasswordHash = "old-hash",
        Role = "User",
        IsActive = true
    };

    _repositoryMock
        .Setup(r => r.GetUserByIdAsync(1))
        .ReturnsAsync(user);

    _passwordHasherMock
        .Setup(h => h.VerifyHashedPassword(
            user, "old-hash", "SamePassword123!"))
        .Returns(PasswordVerificationResult.Success);

    // Act and Assert
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _authService.ChangePasswordAsync(
            1,
            "SamePassword123!",
            "SamePassword123!"));

    _repositoryMock.Verify(
        r => r.ChangePasswordAsync(
            It.IsAny<int>(),
            It.IsAny<string>()),
        Times.Never);
}

}
