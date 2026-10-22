
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.DTOs.Response;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public sealed class AuthService(
    IAuthRepository authRepository,
    IConfiguration configuration,
    IPasswordHasher<LoginUser> passwordHasher)
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

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = Convert.ToBase64String(refreshTokenBytes);
        var refreshTokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var tokenFamilyId = Guid.NewGuid();

        var statusCode = await authRepository.CreateRefreshTokenAsync(
            user.UserId,
            refreshTokenHash,
            tokenFamilyId,
            DateTime.SpecifyKind(refreshTokenExpiry, DateTimeKind.Utc));

        if (statusCode != 0)
            throw new InvalidOperationException(
                "The refresh token could not be saved.");

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
    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
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
    
public async Task<bool> ChangePasswordAsync(
    int userId,
    string currentPassword,
    string newPassword)
{
    // Find the current user using their authenticated user ID.
    var user = await authRepository.GetUserByIdAsync(userId);

    if (user is null || !user.IsActive)
        return false;

    // Verify the current password.
    var verification = passwordHasher.VerifyHashedPassword(
        user,
        user.PasswordHash,
        currentPassword);

    if (verification == PasswordVerificationResult.Failed)
        return false;

    // Prevent changing the password to the same password.
    if (currentPassword == newPassword)
        throw new ArgumentException(
            "The new password must be different from the current password.");

    // Hash the new password before saving it.
    var newPasswordHash = passwordHasher.HashPassword(user, newPassword);

    var rowsAffected = await authRepository.ChangePasswordAsync(
        userId,
        newPasswordHash);

    return rowsAffected > 0;
}

}
