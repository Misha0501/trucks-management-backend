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

        app.MapPut("/rides/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                [FromBody] UpdateRideRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    // ✅ Validate Ride ID
                    if (!Guid.TryParse(id, out var rideGuid))
                    {
                        return ApiResponseFactory.Error("Invalid Ride ID format.", StatusCodes.Status400BadRequest);
                    }

                    // ✅ Find Ride
                    var ride = await db.Rides.Include(r => r.Company).FirstOrDefaultAsync(r => r.Id == rideGuid);
                    if (ride == null)
                    {
                        return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                    }

                    // ✅ Get User Info
                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                    // ✅ If Customer Admin, verify ownership of the current ride's company
                    var associatedCompanyIds = new List<Guid>();
                    if (isCustomerAdmin && !isGlobalAdmin)
                    {
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                        if (contactPerson == null)
                        {
                            return ApiResponseFactory.Error("No contact person profile found. You are not authorized.",
                                StatusCodes.Status403Forbidden);
                        }

                        associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Where(id => id.HasValue) // Remove nulls
                            .Select(id => id.Value)   // Convert to non-nullable Guid
                            .Distinct()
                            .ToList();

                        // Ensure user is authorized to edit this ride
                        if (!associatedCompanyIds.Contains(ride.CompanyId))
                        {
                            return ApiResponseFactory.Error("You are not authorized to edit this ride.",
                                StatusCodes.Status403Forbidden);
                        }
                    }

                    // ✅ Validate & Change Company ID if provided
                    if (!string.IsNullOrWhiteSpace(request.CompanyId))
                    {
                        if (!Guid.TryParse(request.CompanyId, out var newCompanyGuid))
                        {
                            return ApiResponseFactory.Error("Invalid Company ID format.",
                                StatusCodes.Status400BadRequest);
                        }

                        var newCompanyExists = await db.Companies.AnyAsync(c => c.Id == newCompanyGuid);
                        if (!newCompanyExists)
                        {
                            return ApiResponseFactory.Error("The specified company does not exist.",
                                StatusCodes.Status400BadRequest);
                        }

                        // If customerAdmin, ensure they are part of the new company
                        if (isCustomerAdmin && !isGlobalAdmin)
                        {
                            if (!associatedCompanyIds.Contains(newCompanyGuid))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to assign this ride to a new company.",
                                    StatusCodes.Status403Forbidden);
                            }
                        }

                        ride.CompanyId = newCompanyGuid;
                    }

                    // ✅ Update Other Fields
                    if (!string.IsNullOrWhiteSpace(request.Name)) ride.Name = request.Name;
                    if (!string.IsNullOrWhiteSpace(request.Remark)) ride.Remark = request.Remark;

                    await db.SaveChangesAsync();

                    var updatedRide = await db.Rides
                        .Include(r => r.Company)
                        .FirstOrDefaultAsync(r => r.Id == rideGuid);

                    var responseData = new
                    {
                        updatedRide.Id,
                        updatedRide.Name,
                        updatedRide.Remark,
                        updatedRide.CompanyId,
                        CompanyName = updatedRide.Company.Name
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error updating ride: {ex.Message}");
                    return ApiResponseFactory.Error("An unexpected error occurred while updating the ride.",
                        StatusCodes.Status500InternalServerError);
                }
            });


        return app;
    }
}