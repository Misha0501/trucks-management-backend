using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class HoursOptionRoutes
    {
        public static void MapHoursOptionRoutes(this WebApplication app)
        {
            app.MapGet("/hoursoptions",
                [Authorize] // only authenticated users can call this
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        var hoursOptions = await db.HoursOptions
                            .AsNoTracking()
                            .ToListAsync();

                        var responseData = hoursOptions.Select(ho => new
                        {
                            ho.Id,
                            ho.Name,
                            ho.IsActive
                        });

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching HoursOptions: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while retrieving HoursOptions.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                }
            );

            app.MapGet("/hoursoptions/{id}",
                [Authorize] async (
                    string id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (!Guid.TryParse(id, out Guid parsedId))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid HoursOption ID format.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        var hoursOption = await db.HoursOptions
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ho => ho.Id == parsedId);

                        if (hoursOption == null)
                        {
                            return ApiResponseFactory.Error(
                                "HoursOption not found.",
                                StatusCodes.Status404NotFound
                            );
                        }

                        var responseData = new
                        {
                            hoursOption.Id,
                            hoursOption.Name,
                            hoursOption.IsActive
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching HoursOption by ID: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while retrieving the HoursOption.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                }
            );
        }
    }
}
