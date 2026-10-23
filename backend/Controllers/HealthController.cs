
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            message = "Keells Product Catalogue API is running."
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> CheckDatabase()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            return Ok(new
            {
                status = "Connected"
            });
        }
        catch (SqlException)
        {
            return StatusCode(503, new
            {
                status = "Unavailable",
                message = "Could not connect to the database."
            });
        }
    }
}
