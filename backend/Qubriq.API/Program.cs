using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Qubriq.API.Data;
using Microsoft.AspNetCore.Authorization;
using Qubriq.API.Authorization;
using Qubriq.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ==========================================
// MYSQL DATABASE CONFIGURATION
// ==========================================

var connectionString = builder.Configuration
    .GetConnectionString("QubriqDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "QubriqDatabase connection string is missing.");
}

builder.Services.AddDbContext<QubriqDbContext>(options =>
    options.UseMySQL(connectionString));
// ==========================================
// APPLICATION SERVICES
// ==========================================

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ==========================================
// JWT AUTHENTICATION CONFIGURATION
// ==========================================

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtSigningKey) ||
    string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT configuration is incomplete. Check .NET User Secrets.");
}

byte[] signingKeyBytes;

try
{
    signingKeyBytes = Convert.FromBase64String(jwtSigningKey);
}
catch (FormatException)
{
    throw new InvalidOperationException(
        "JWT signing key must be valid Base64.");
}

if (signingKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must contain at least 32 bytes.");
}

var signingKey = new SymmetricSecurityKey(signingKeyBytes);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,
                RequireExpirationTime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                ValidAlgorithms = new[]
                {
                    SecurityAlgorithms.HmacSha256
                }
            };
    });

// ==========================================
// AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BookingsRead", policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(
                "bookings.read_all",
                "bookings.read_own")));

    options.AddPolicy("BookingsReadAll", policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(
                "bookings.read_all")));

    options.AddPolicy("BookingsCreate", policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(
                "bookings.create")));
    options.AddPolicy("AdminCustomerProvision", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");

        // Both permissions are required.
        policy.AddRequirements(
            new PermissionRequirement("users.create"));

        policy.AddRequirements(
            new PermissionRequirement("users.assign_role"));
    });
});

// ==========================================
// CORS CONFIGURATION
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("QubriqFrontend", policy =>
    {
        policy
            .WithOrigins("http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ==========================================
// BUILD APPLICATION
// ==========================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("QubriqFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ==========================================
// DATABASE HEALTH ENDPOINT
// ==========================================

app.MapGet("/api/database/health",
    async (QubriqDbContext dbContext) =>
    {
        try
        {
            await dbContext.Database.OpenConnectionAsync();
            await dbContext.Database.CloseConnectionAsync();

            return Results.Ok(new
            {
                database = "MySQL",
                status = "Connected",
                databaseName = "qubriq_dev"
            });
        }
        catch (Exception ex)
        {
            app.Logger.LogError(
                ex,
                "Database connectivity check failed.");

            return Results.Problem(
                title: "Database connection failed",
                detail: app.Environment.IsDevelopment()
                    ? ex.GetBaseException().Message
                    : null,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    });

// ==========================================
// ROOT ENDPOINT
// ==========================================

app.MapGet("/", () => Results.Ok(new
{
    application = "Qubriq API",
    status = "Running",
    version = "0.1.0",
    stage = 2
}));

app.Run();
