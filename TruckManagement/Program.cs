using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Extensions;
using TruckManagement.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Seeding;
using TruckManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services
builder.Services.AddOpenApi();
builder.Services.AddPostgresDatabase(builder.Configuration);
builder.Services.AddAppIdentity();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins("https://peaceful-liskov.85-214-92-159.plesk.page", "http://localhost:4000","https://localhost:4000", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Use cors
app.UseCors("AllowedOrigins");

// Global Exception Handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Apply migrations and seed data at startup
using (var scope = app.Services.CreateScope())
{
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Register endpoints from separate modules
app.MapAuthEndpoints();
app.MapWeatherForecastEndpoints();
app.MapUserEndpoints();
app.MapCompanyEndpoints();
app.MapRoleEndpoints();

app.Run();
