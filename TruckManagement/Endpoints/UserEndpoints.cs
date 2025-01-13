using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
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
        
        app.MapPost("/users/change-password", async (
                ChangePasswordRequest req,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext
            ) =>
            {
                // 1) Get the user’s ID from JWT claims (e.g., "sub")
                //    The GenerateJwtToken typically sets user.Id in JwtRegisteredClaimNames.Sub
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiResponseFactory.Error("Invalid token or user ID not found.", StatusCodes.Status401Unauthorized);
                }

                // 2) Retrieve the user
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);
                }

                // 3) Check new/confirm match
                if (req.NewPassword != req.ConfirmNewPassword)
                {
                    return ApiResponseFactory.Error("New password and confirmation do not match.", StatusCodes.Status400BadRequest);
                }

                // 4) Attempt to change the password
                var result = await userManager.ChangePasswordAsync(user, req.OldPassword, req.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                }

                // 5) Return success
                return ApiResponseFactory.Success("Password changed successfully.", StatusCodes.Status200OK);
            })
            .RequireAuthorization(); // Must be logged in to change password
        
        // GET /users (Paginated)
        // GET /users => Paginated list of users (with roles)
        app.MapGet("/users",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                // Required parameter first
                ApplicationDbContext db,
                // Optional parameters after
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize   = 10
            ) =>
            {
                // 1) Count total users for pagination
                var totalUsers = await db.Users.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

                // 2) Query for users (paged, with roles)
                var pagedUsers = await db.Users
                    .AsNoTracking()
                    .OrderBy(u => u.Email)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new 
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        u.CompanyId,
                        Roles = (from ur in db.UserRoles
                            join r in db.Roles on ur.RoleId equals r.Id
                            where ur.UserId == u.Id
                            select r.Name).ToList()
                    })
                    .ToListAsync();

                // 3) Build a paginated response object
                var responseData = new
                {
                    totalUsers,
                    totalPages,
                    pageNumber,
                    pageSize,
                    data = pagedUsers
                };

                // 4) Return via your custom ApiResponseFactory
                return ApiResponseFactory.Success(
                    responseData,
                    StatusCodes.Status200OK
                );
            }
        );


        return app;
    }
}
