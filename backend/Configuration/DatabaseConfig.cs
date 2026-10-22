
using Microsoft.Data.SqlClient;

namespace backend.Configuration;

public static class DatabaseConfig
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The database connection string is missing.");
        }

        services.AddScoped<SqlConnection>(_ =>
            new SqlConnection(connectionString));

        return services;
    }
}
