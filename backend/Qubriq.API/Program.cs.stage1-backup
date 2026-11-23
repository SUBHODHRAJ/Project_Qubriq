var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

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

app.MapGet("/", () => Results.Ok(new
{
    application = "Qubriq API",
    status = "Running",
    version = "0.1.0",
    stage = 0
}));

app.Run();
