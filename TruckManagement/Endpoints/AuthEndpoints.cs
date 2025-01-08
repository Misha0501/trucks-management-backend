using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;

namespace TruckManagement.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/register", async (
            RegisterRequest req,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) => 
        {
            // 1) Validate if 'companyId' is a correct GUID
            if (!Guid.TryParse(req.CompanyId.ToString(), out var parsedCompanyId))
            {
                return Results.BadRequest("The provided Company ID is not a valid GUID.");
            }

            // 2) Check if the company exists in the database
            var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == parsedCompanyId);
            if (!companyExists)
            {
                return Results.BadRequest("The specified company does not exist. Please provide a valid Company ID.");
            }

            // 3) Create the user (now that we know the company is valid)
            var user = new ApplicationUser 
            {
                UserName = req.Email,
                Email = req.Email,
                FirstName = req.FirstName,
                LastName = req.LastName,
                CompanyId = parsedCompanyId
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded) 
                return Results.BadRequest(result.Errors);

            return Results.Ok("User registered successfully.");
        });

        app.MapPost("/login", async (
            LoginRequest req,
            UserManager<ApplicationUser> userManager,
            IConfiguration config) => 
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user == null) return Results.BadRequest("Invalid credentials");

            var isCorrectPassword = await userManager.CheckPasswordAsync(user, req.Password);
            if (!isCorrectPassword) return Results.BadRequest("Invalid credentials");

            // Generate a JWT token:
            var token = JwtTokenHelper.GenerateJwtToken(user, config); 
            return Results.Ok(new { token });
        });

        return app;
    }
}