using System.Globalization;
using Microsoft.AspNetCore.Localization;
using TruckManagement.Api.Endpoints;
using TruckManagement.Endpoints;
using TruckManagement.Extensions;
using TruckManagement.Options;
using TruckManagement.Routes;
using TruckManagement.Seeding;
using TruckManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
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
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddScoped<DriverCompensationService>();
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection("Storage"));

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddScoped<IResourceLocalizer, ResourceLocalizer>();

// Define supported cultures
var supportedCultures = new[] 
{ 
    new CultureInfo("en"), 
    new CultureInfo("nl"),
    new CultureInfo("bg") 
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");

    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});


var app = builder.Build();
app.UseRequestLocalization();

// Use cors
app.UseCors("AllowAll");

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
app.MapUserEndpoints();
app.MapCompanyEndpoints();
app.MapRoleEndpoints();
app.RegisterClientsRoutes();
app.MapDriversEndpoints();
app.MapContactPersonsEndpoints();
app.MapSurchargeEndpoints();
app.MapRateEndpoints();
app.MapUnitEndpoints();
app.MapCarEndpoints();
app.MapCharterEndpoints();
app.MapRideEndpoints();
app.MapPartRideEndpoints();
app.MapHoursCodeRoutes();
app.MapHoursOptionRoutes();
app.MapEmployeeContractsEndpoints();
app.MapFileUploadsEndpoints();
app.MapPartRideFilesEndpoints();
app.MapDisputeEndpoints();
app.MapWeekToSubmitEndpoints();

app.Run();
