
using System.Data;
using System.Security;
using backend.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== First Admin Setup ===");

var username = ReadUsername();
var password = ReadPassword("Enter Admin password: ");
var confirmPassword = ReadPassword("Confirm Admin password: ");

if (password != confirmPassword)
{
    Console.WriteLine("Passwords do not match.");
    return;
}

if (password.Length < 12)
{
    Console.WriteLine("Use a password with at least 12 characters.");
    return;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(
        Path.GetFullPath(
            @"backend\appsettings.json",
            Directory.GetCurrentDirectory()),
        optional: false)
        .AddUserSecrets(
    typeof(backend.Models.LoginUser).Assembly,
    optional: true)
    .Build();

var connectionString =
    configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Database connection string is missing.");
    Console.WriteLine(
        "Configure ConnectionStrings:DefaultConnection for AdminSetup.");
    return;
}

var user = new LoginUser { Username = username };
var passwordHasher = new PasswordHasher<LoginUser>();
var passwordHash = passwordHasher.HashPassword(user, password);

await using var connection = new SqlConnection(connectionString);

try
{
    await connection.OpenAsync();

    var statusCode = await connection.QuerySingleAsync<int>(
        "dbo.usp_Users_BootstrapFirstAdmin",
        new
        {
            Username = username,
            PasswordHash = passwordHash
        },
        commandType: CommandType.StoredProcedure);

    switch (statusCode)
    {
        case 0:
            Console.WriteLine("First Admin account created successfully.");
            Console.WriteLine("You can now log in through the API.");
            break;

        case 1:
            Console.WriteLine(
                "Setup refused: a user already exists in the database.");
            break;

        case 2:
            Console.WriteLine("Invalid username or password hash.");
            break;

        default:
            Console.WriteLine(
                $"Unexpected status code: {statusCode}");
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Admin setup failed: {ex.Message}");
}

static string ReadUsername()
{
    Console.Write("Admin username: ");
    return (Console.ReadLine() ?? string.Empty).Trim();
}

static string ReadPassword(string prompt)
{
    Console.Write(prompt);
    var password = new System.Text.StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
                password.Length--;

            continue;
        }

        if (!char.IsControl(key.KeyChar))
            password.Append(key.KeyChar);
    }

    return password.ToString();
}
