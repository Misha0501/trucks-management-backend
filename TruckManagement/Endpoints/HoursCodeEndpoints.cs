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
    public static class HoursCodeEndpoints
    {
        public static void MapHoursCodeRoutes(this WebApplication app)
        {
            app.MapGet("/hourscodes",
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

                        var hoursCodes = await db.HoursCodes
                            .AsNoTracking()
                            .ToListAsync();

                        var responseData = hoursCodes.Select(hc => new
                        {
                            hc.Id,
                            hc.Name,
                            hc.IsActive
                        });

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching HoursCodes: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while retrieving HoursCodes.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                }
            );

            app.MapGet("/hourscodes/{id}",
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
                                "Invalid HoursCode ID format.",
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

                        var hoursCode = await db.HoursCodes
                            .AsNoTracking()
                            .FirstOrDefaultAsync(hc => hc.Id == parsedId);

                        if (hoursCode == null)
                        {
                            return ApiResponseFactory.Error(
                                "HoursCode not found.",
                                StatusCodes.Status404NotFound
                            );
                        }

                        var responseData = new
                        {
                            hoursCode.Id,
                            hoursCode.Name,
                            hoursCode.IsActive
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching HoursCode by ID: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while retrieving the HoursCode.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                }
            );
        }
    }
}
