using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints;

public static class RideEndpoints
{
    public static WebApplication MapRideEndpoints(this WebApplication app)
    {
        app.MapPost("/rides",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                [FromBody] CreateRideRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (request == null || string.IsNullOrWhiteSpace(request.Name))
                    {
                        return ApiResponseFactory.Error(
                            "Ride name is required.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    if (string.IsNullOrWhiteSpace(request.CompanyId))
                    {
                        return ApiResponseFactory.Error(
                            "CompanyId is required.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    if (!Guid.TryParse(request.CompanyId, out var companyGuid))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid CompanyId format.",
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

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                    // Fetch the company
                    var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyGuid);
                    if (company == null)
                    {
                        return ApiResponseFactory.Error(
                            "The specified company does not exist.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // If not global admin, check if user is associated with this company
                    if (!isGlobalAdmin)
                    {
                        // Load the contact person bridging for this user
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                        if (contactPerson == null)
                        {
                            return ApiResponseFactory.Error(
                                "No contact person profile found. You are not authorized.",
                                StatusCodes.Status403Forbidden
                            );
                        }

                        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Distinct()
                            .ToList();

                        // Ensure user is associated with the provided company
                        if (!associatedCompanyIds.Contains(companyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "You are not authorized to create a ride in this company.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // Create the ride
                    var ride = new Ride
                    {
                        Id = Guid.NewGuid(),
                        Name = request.Name,
                        Remark = request.Remark,
                        CompanyId = companyGuid
                    };

                    db.Rides.Add(ride);
                    await db.SaveChangesAsync();

                    var responseData = new
                    {
                        ride.Id,
                        ride.Name,
                        ride.Remark,
                        ride.CompanyId
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status201Created);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error creating ride: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while creating the ride.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        return app;
    }
}