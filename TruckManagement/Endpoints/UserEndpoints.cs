using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TruckManagement.Entities;
using TruckManagement.Helpers;  
using TruckManagement.Models;   

namespace TruckManagement.Endpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        // GET /users/me -> return info about the current authenticated user
        app.MapGet("/users/me", async (
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager) =>
        {
            // 1) Retrieve user's email claim from the JWT
            var userEmail = httpContext.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return ApiResponseFactory.Error(
                    "User is not authenticated or no email claim found.",
                    StatusCodes.Status401Unauthorized
                );
            }

            // 2) Get the user from the database
            var user = await userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return ApiResponseFactory.Error(
                    "No user found with the provided credentials.",
                    StatusCodes.Status401Unauthorized
                );
            }

            // 3) Retrieve user roles
            var roles = await userManager.GetRolesAsync(user);

            // 4) Prepare the returned data
            var data = new
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                CompanyId = user.CompanyId
            };

            // 5) Return a standardized success response
            return ApiResponseFactory.Success(
                data: data,
                statusCode: StatusCodes.Status200OK
            );
        })
        .RequireAuthorization(); // Only authenticated users

        return app;
    }
}
