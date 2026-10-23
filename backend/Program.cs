
using System.Text;
using backend.Configuration;
using backend.ExternalServices;
using backend.Middleware;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// CORS: allow the React development frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Repositories
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IProductService, ProductService>();

// In-memory cache for exchange rates
builder.Services.AddMemoryCache();

// Typed HTTP client for ExchangeRate-API
builder.Services.AddHttpClient<IExchangeRateService, ExchangeRateService>(
    client =>
    {
        client.BaseAddress = new Uri(
            "https://v6.exchangerate-api.com/v6/");

        client.Timeout = TimeSpan.FromSeconds(10);
    });

// Cached exchange-rate service
builder.Services.AddScoped<CachedExchangeRateService>();

// Swagger authentication configuration
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [
                new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                    "Bearer", document)
            ] = []
        });
});

// Database configuration
builder.Services.AddDatabase(builder.Configuration);

// Existing concrete registrations
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<AuthService>();

// Password hashing
builder.Services.AddScoped<
    IPasswordHasher<LoginUser>,
    PasswordHasher<LoginUser>>();

// Read JWT configuration
var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtSecret) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT configuration is missing. Configure Jwt:Secret, " +
        "Jwt:Issuer, and Jwt:Audience using User Secrets.");
}

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSecret));

// JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger is available in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must run before authentication and authorization.
app.UseCors("FrontendPolicy");

// Authenticate the request and populate HttpContext.User.
app.UseAuthentication();

// Check the user's current password-change status in the database.
app.UseMiddleware<PasswordChangeMiddleware>();

// Apply [Authorize] and role-based authorization.
app.UseAuthorization();

app.MapControllers();

app.Run();
