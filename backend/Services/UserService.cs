
using System.Security.Cryptography;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;

namespace backend.Services;

public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher<LoginUser> passwordHasher) : IUserService
{
    public async Task<(int StatusCode, int? UserId, string? TemporaryPassword)>
        CreateUserAsync(
            int performedByUserId,
            string username,
            string role)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            username.Trim().Length < 3 ||
            username.Trim().Length > 50 ||
            !IsValidRole(role))
        {
            return (1, null, null);
        }

        username = username.Trim();

        // Generate a temporary password securely.
        var temporaryPassword = GenerateTemporaryPassword();

        // Hash the password before saving it.
        var user = new LoginUser
        {
            Username = username,
            Role = role
        };

        var passwordHash = passwordHasher.HashPassword(
            user,
            temporaryPassword);

        var (statusCode, userId) = await userRepository.CreateUserAsync(
            performedByUserId,
            username,
            passwordHash,
            role);

        // Return the password only when creation succeeds.
        return statusCode == 0
            ? (statusCode, userId, temporaryPassword)
            : (statusCode, null, null);
    }

    public Task<int> UpdateUserAsync(
    int performedByUserId,
    int userId,
    string username,
    string role)
{
    if (userId <= 0 ||
        string.IsNullOrWhiteSpace(username) ||
        username.Trim().Length < 3 ||
        username.Trim().Length > 50 ||
        !IsValidRole(role))
    {
        return Task.FromResult(1);
    }

    // Prevent an administrator from demoting themselves.
    if (performedByUserId == userId && role != "Admin")
    {
        return Task.FromResult(5);
    }

    return userRepository.UpdateUserAsync(
        performedByUserId,
        userId,
        username.Trim(),
        role);
}

    public async Task<(int StatusCode, string? TemporaryPassword)>
        ResetUserPasswordAsync(
            int performedByUserId,
            int userId)
    {
        if (userId <= 0)
        {
            return (1, null);
        }

        // Generate a new temporary password for the reset.
        var temporaryPassword = GenerateTemporaryPassword();

        var user = new LoginUser
        {
            UserId = userId
        };

        var passwordHash = passwordHasher.HashPassword(
            user,
            temporaryPassword);

        var statusCode = await userRepository.ResetUserPasswordAsync(
            performedByUserId,
            userId,
            passwordHash);

        // Never return a generated password when the reset fails.
        return statusCode == 0
            ? (statusCode, temporaryPassword)
            : (statusCode, null);
    }

    public Task<int> ActivateUserAsync(
        int performedByUserId,
        int userId)
    {
        if (userId <= 0)
        {
            return Task.FromResult(1);
        }

        return userRepository.ActivateUserAsync(
            performedByUserId,
            userId);
    }

    public Task<int> DeactivateUserAsync(
        int performedByUserId,
        int userId)
    {
        if (userId <= 0)
        {
            return Task.FromResult(1);
        }

        return userRepository.DeactivateUserAsync(
            performedByUserId,
            userId);
    }

    public Task<IReadOnlyList<UserListItem>> GetAllUsersAsync(
        int performedByUserId)
    {
        return userRepository.GetAllUsersAsync(performedByUserId);
    }

    private static bool IsValidRole(string role) =>
        role is "Admin" or "User";

    private static string GenerateTemporaryPassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%*-_";

        const string allCharacters =
            uppercase + lowercase + digits + symbols;

        // Guarantee at least one character from each category.
        var characters = new List<char>
        {
            GetRandomCharacter(uppercase),
            GetRandomCharacter(lowercase),
            GetRandomCharacter(digits),
            GetRandomCharacter(symbols)
        };

        // Generate a total of 16 characters.
        while (characters.Count < 16)
        {
            characters.Add(GetRandomCharacter(allCharacters));
        }

        // Shuffle using a cryptographically secure random generator.
        for (var i = characters.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (characters[i], characters[j]) =
                (characters[j], characters[i]);
        }

        return new string(characters.ToArray());
    }

    private static char GetRandomCharacter(string characters)
    {
        var index = RandomNumberGenerator.GetInt32(characters.Length);
        return characters[index];
    }
}
