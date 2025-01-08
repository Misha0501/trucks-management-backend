using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;

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
            // 1) Validate if 'companyId' is a correct GUID (if your DTO uses a string).
            if (!Guid.TryParse(req.CompanyId, out var parsedCompanyId))
            {
                return ApiResponseFactory.Error(
                    "The provided Company ID is not a valid GUID.", 
                    StatusCodes.Status400BadRequest
                );
            }

            // 2) Check if the company exists in the database
            var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == parsedCompanyId);
            if (!companyExists)
            {
                return ApiResponseFactory.Error(
                    "The specified company does not exist. Please provide a valid Company ID.",
                    StatusCodes.Status400BadRequest
                );
            }

            // 3) Create the user
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
            {
                // Consolidate Identity errors into a single string or multiple
                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return ApiResponseFactory.Error(errorMessages, StatusCodes.Status400BadRequest);
            }

            // Return a success response
            return ApiResponseFactory.Success("User registered successfully.", StatusCodes.Status200OK);
        });

        app.MapPost("/login", async (
            LoginRequest req,
            UserManager<ApplicationUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user == null)
            {
                return ApiResponseFactory.Error("Invalid credentials.", StatusCodes.Status400BadRequest);
            }

            var isCorrectPassword = await userManager.CheckPasswordAsync(user, req.Password);
            if (!isCorrectPassword) 
            {
                return ApiResponseFactory.Error("Invalid credentials.", StatusCodes.Status400BadRequest);
            }

            var token = JwtTokenHelper.GenerateJwtToken(user, config);
            var data = new { token };

            return ApiResponseFactory.Success(data, StatusCodes.Status200OK);
        });

        return app;
    }
}