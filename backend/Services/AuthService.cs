
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.DTOs.Responses;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public sealed class AuthService(
    IAuthRepository authRepository,
    IConfiguration configuration,
    IPasswordHasher<LoginUser> passwordHasher) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(
        string username,
        string password)
    {
        var user = await authRepository.GetUserByUsernameAsync(username);

        if (user is null || !user.IsActive)
            return null;

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (verification == PasswordVerificationResult.Failed)
            return null;

        var now = DateTime.UtcNow;
        var accessTokenExpiry = now.AddMinutes(15);
        var refreshTokenExpiry = now.AddDays(7);

        var accessToken = GenerateAccessToken(user, accessTokenExpiry);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashRefreshToken(refreshToken);

        var statusCode = await authRepository.CreateRefreshTokenAsync(
            user.UserId,
            refreshTokenHash,
            Guid.NewGuid(),
            DateTime.SpecifyKind(refreshTokenExpiry, DateTimeKind.Utc));

        if (statusCode != 0)
            throw new InvalidOperationException(
                "The refresh token could not be saved.");

        return CreateLoginResponse(
            user,
            accessToken,
            refreshToken,
            accessTokenExpiry);
    }

    public async Task<LoginResponse?> RefreshTokenAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var newRefreshToken = GenerateRefreshToken();

        var rotationResult = await authRepository.RotateRefreshTokenAsync(
            HashRefreshToken(refreshToken),
            HashRefreshToken(newRefreshToken),
            DateTime.UtcNow.AddDays(7));

        // Only a successful rotation can issue new tokens.
        if (rotationResult.StatusCode != 0 ||
            rotationResult.UserId is null)
        {
            return null;
        }

        var user = await authRepository.GetUserByIdAsync(
            rotationResult.UserId.Value);

        if (user is null || !user.IsActive)
            return null;

        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        var accessToken = GenerateAccessToken(user, accessTokenExpiry);

        return CreateLoginResponse(
            user,
            accessToken,
            newRefreshToken,
            accessTokenExpiry);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var statusCode =
            await authRepository.RevokeRefreshTokenFamilyAsync(
                HashRefreshToken(refreshToken));

        return statusCode == 0;
    }

    public async Task<bool> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword)
    {
        var user = await authRepository.GetUserByIdAsync(userId);

        if (user is null || !user.IsActive)
            return false;

        var verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            currentPassword);

        if (verification == PasswordVerificationResult.Failed)
            return false;

        if (currentPassword == newPassword)
            throw new ArgumentException(
                "The new password must be different from the current password.");

        var newPasswordHash = passwordHasher.HashPassword(user, newPassword);

        var rowsAffected = await authRepository.ChangePasswordAsync(
            userId,
            newPasswordHash);

        return rowsAffected > 0;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    private static LoginResponse CreateLoginResponse(
        LoginUser user,
        string accessToken,
        string refreshToken,
        DateTime accessTokenExpiry)
    {
        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = accessTokenExpiry,
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword
        };
    }

    private string GenerateAccessToken(
        LoginUser user,
        DateTime expiresAtUtc)
    {
        var secret = configuration["Jwt:Secret"];
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(secret) ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "JWT configuration is missing.");
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(
                "must_change_password",
                user.MustChangePassword.ToString().ToLowerInvariant())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
