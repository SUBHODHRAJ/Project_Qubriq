using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// MySQL database configuration
var connectionString = builder.Configuration
    .GetConnectionString("QubriqDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "QubriqDatabase connection string is missing.");
}

builder.Services.AddDbContext<QubriqDbContext>(options =>
    options.UseMySQL(connectionString));

// CORS configuration
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("QubriqFrontend");

app.MapControllers();

// Database connectivity endpoint
app.MapGet("/api/database/health",
    async (QubriqDbContext dbContext) =>
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();

            return canConnect
                ? Results.Ok(new
                {
                    database = "MySQL",
                    status = "Connected",
                    databaseName = "qubriq_dev"
                })
                : Results.Problem(
                    title: "Database unavailable",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch
        {
            return Results.Problem(
                title: "Database connection failed",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    });

app.MapGet("/", () => Results.Ok(new
{
    application = "Qubriq API",
    status = "Running",
    version = "0.1.0",
    stage = 1
}));

app.Run();
