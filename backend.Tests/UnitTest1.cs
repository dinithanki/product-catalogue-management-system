
using backend.DTOs.Responses;
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

    private static LoginUser CreateActiveUser() => new()
    {
        UserId = 1,
        Username = "testuser",
        PasswordHash = "fake-password-hash",
        Role = "User",
        IsActive = true,
        MustChangePassword = false
    };

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsLoginResponse()
    {
        var user = CreateActiveUser();

        _repositoryMock
            .Setup(r => r.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(
                user, user.PasswordHash, "CorrectPassword123!"))
            .Returns(PasswordVerificationResult.Success);

        _repositoryMock
            .Setup(r => r.CreateRefreshTokenAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        string? refreshCookie = null;
        var result = await _authService.LoginAsync(
            "testuser", "CorrectPassword123!", (token, _) => refreshCookie = token);

        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal(1, result.UserId);
        Assert.Equal("User", result.Role);
        Assert.False(result.MustChangePassword);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshCookie));
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsNull()
    {
        var user = CreateActiveUser();

        _repositoryMock
            .Setup(r => r.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(
                user, user.PasswordHash, "WrongPassword123!"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _authService.LoginAsync(
            "testuser", "WrongPassword123!", (_, _) => { });

        Assert.Null(result);

        _repositoryMock.Verify(
            r => r.CreateRefreshTokenAsync(
                It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<Guid>(), It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ReturnsNull()
    {
        var user = CreateActiveUser();
        user.IsActive = false;

        _repositoryMock
            .Setup(r => r.GetUserByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        var result = await _authService.LoginAsync(
            "testuser", "CorrectPassword123!", (_, _) => { });

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
        var user = CreateActiveUser();
        user.PasswordHash = "old-hash";
        user.MustChangePassword = true;

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

        var result = await _authService.ChangePasswordAsync(
            1, "OldPassword123!", "NewPassword123!");

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
        var user = CreateActiveUser();
        user.PasswordHash = "old-hash";

        _repositoryMock
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(
                user, "old-hash", "WrongPassword123!"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _authService.ChangePasswordAsync(
            1, "WrongPassword123!", "NewPassword123!");

        Assert.False(result);

        _passwordHasherMock.Verify(
            h => h.HashPassword(
                It.IsAny<LoginUser>(), It.IsAny<string>()),
            Times.Never);

        _repositoryMock.Verify(
            r => r.ChangePasswordAsync(
                It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInactiveUser_ReturnsFalse()
    {
        var user = CreateActiveUser();
        user.IsActive = false;

        _repositoryMock
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        var result = await _authService.ChangePasswordAsync(
            1, "OldPassword123!", "NewPassword123!");

        Assert.False(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithSamePassword_ThrowsArgumentException()
    {
        var user = CreateActiveUser();
        user.PasswordHash = "old-hash";

        _repositoryMock
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyHashedPassword(
                user, "old-hash", "SamePassword123!"))
            .Returns(PasswordVerificationResult.Success);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _authService.ChangePasswordAsync(
                1, "SamePassword123!", "SamePassword123!"));

        _repositoryMock.Verify(
            r => r.ChangePasswordAsync(
                It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.RotateRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new RefreshTokenRotationResult(1, null, null));

        var result = await _authService.RefreshTokenAsync(
            "invalid-refresh-token", (_, _) => { });

        Assert.Null(result);

        _repositoryMock.Verify(
            r => r.GetUserByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        var user = CreateActiveUser();

        _repositoryMock
            .Setup(r => r.RotateRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new RefreshTokenRotationResult(
                0, 1, Guid.NewGuid()));

        _repositoryMock
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        string? replacementCookie = null;
        var result = await _authService.RefreshTokenAsync(
            "valid-refresh-token", (token, _) => replacementCookie = token);

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(replacementCookie));
        Assert.NotEqual("valid-refresh-token", replacementCookie);

        _repositoryMock.Verify(
            r => r.RotateRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInactiveUser_ReturnsNull()
    {
        var user = CreateActiveUser();
        user.IsActive = false;

        _repositoryMock
            .Setup(r => r.RotateRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new RefreshTokenRotationResult(
                0, 1, Guid.NewGuid()));

        _repositoryMock
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(user);

        var result = await _authService.RefreshTokenAsync(
            "valid-refresh-token", (_, _) => { });

        Assert.Null(result);
    }

    [Fact]
    public async Task LogoutAsync_WithValidToken_ReturnsTrue()
    {
        _repositoryMock
            .Setup(r => r.RevokeRefreshTokenFamilyAsync(
                It.IsAny<string>()))
            .ReturnsAsync(0);

        var result = await _authService.LogoutAsync(
            "valid-refresh-token");

        Assert.True(result);

        _repositoryMock.Verify(
            r => r.RevokeRefreshTokenFamilyAsync(
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WhenRevocationFails_ReturnsFalse()
    {
        _repositoryMock
            .Setup(r => r.RevokeRefreshTokenFamilyAsync(
                It.IsAny<string>()))
            .ReturnsAsync(1);

        var result = await _authService.LogoutAsync(
            "invalid-refresh-token");

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithEmptyToken_ReturnsNull()
    {
        var result = await _authService.RefreshTokenAsync("", (_, _) => { });

        Assert.Null(result);

        _repositoryMock.Verify(
            r => r.RotateRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WithEmptyToken_ReturnsFalse()
    {
        var result = await _authService.LogoutAsync("");

        Assert.False(result);

        _repositoryMock.Verify(
            r => r.RevokeRefreshTokenFamilyAsync(
                It.IsAny<string>()),
            Times.Never);
    }
}
