using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Extensions;
using TruckManagement.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddAuthorization();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Use cors
app.UseCors("AllowLocalhost3000");

// Global Exception Handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Apply migrations and seed data at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var roles = new [] { "employee", "employer", "customer", "customerAdmin", "customerBookhourder", "globalAdmin" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = role });
        }
    }

    // Seed a default company
    var defaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    if (!dbContext.Companies.Any(c => c.Id == defaultCompanyId))
    {
        dbContext.Companies.Add(new Company
        {
            Id = defaultCompanyId,
            Name = "DefaultCompany"
        });
        await dbContext.SaveChangesAsync();
    }
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

app.Run();
