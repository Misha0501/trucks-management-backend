using Microsoft.AspNetCore.Identity;
using TruckManagement.DTOs;
using TruckManagement.Entities;

namespace TruckManagement.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/register", async (
            RegisterRequest req,
            UserManager<ApplicationUser> userManager) => 
        {
            var user = new ApplicationUser 
            {
                UserName = req.Email,
                Email = req.Email,
                FirstName = req.FirstName,
                LastName = req.LastName,
                CompanyId = req.CompanyId
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