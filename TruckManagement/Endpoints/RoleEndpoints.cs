using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Entities;
using TruckManagement.Helpers; // Where ApiResponseFactory is
using TruckManagement.Models;  // Where ApiResponse<T> is

namespace TruckManagement.Endpoints;

public static class RoleEndpoints
{
    public static WebApplication MapRoleEndpoints(this WebApplication app)
    {
        // GET /roles -> retrieve all roles
        app.MapGet("/roles", async (RoleManager<ApplicationRole> roleManager) =>
            {
                // Query roles from the database
                // Select only the fields you want to return (e.g., Id, Name)
                var roles = await roleManager.Roles
                    .Select(r => new { r.Id, r.Name })
                    .ToListAsync();

                // Return them as a standardized success response
                return ApiResponseFactory.Success(roles);
            })
            .RequireAuthorization();

        return app;
    }
}