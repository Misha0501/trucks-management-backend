using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Interfaces;

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
                            .Select(id => id.Value) // Convert to non-nullable Guid
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

        app.MapGet("/rides",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
            async (
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 1000
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

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                    bool isEmployer = currentUser.IsInRole("employer");
                    bool isContactPerson = isCustomerAdmin || isEmployer;

                    var rideQuery = db.Rides.AsQueryable();

                    if (isContactPerson)
                    {
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

                        // Convert associatedCompanyIds (from ContactPersonClientCompanies) to List<Guid>
                        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Where(g => g.HasValue) // Filter out nulls
                            .Select(g => g.Value) // Convert to Guid
                            .Distinct()
                            .ToList();

                        // Convert associatedClientIds similarly
                        var associatedClientIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.ClientId)
                            .Where(cid => cid.HasValue)
                            .Select(cid => cid.Value)
                            .Distinct()
                            .ToList();

                        var ownedCompanyIdsFromClients = await db.Clients
                            .Where(c => associatedClientIds.Contains(c.Id))
                            .Select(c => c.CompanyId)
                            .Distinct()
                            .ToListAsync();

                        // Concatenate the two lists explicitly as IEnumerable<Guid>
                        var accessibleCompanyIds = associatedCompanyIds
                            .Concat<Guid>(ownedCompanyIdsFromClients)
                            .Distinct()
                            .ToList();

                        rideQuery = rideQuery.Where(r => accessibleCompanyIds.Contains(r.CompanyId));
                    }

                    var totalRides = await rideQuery.CountAsync();
                    var totalPages = (int)Math.Ceiling((double)totalRides / pageSize);

                    var pagedRides = await rideQuery
                        .OrderBy(r => r.Name)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(r => new
                        {
                            r.Id,
                            r.Name,
                            r.CompanyId,
                            CompanyName = r.Company.Name,
                            r.Remark
                        })
                        .ToListAsync();

                    var responseData = new
                    {
                        totalRides,
                        totalPages,
                        pageNumber,
                        pageSize,
                        data = pagedRides
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error listing rides: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while listing rides.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        app.MapGet("/rides/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    // Validate ride ID format
                    if (!Guid.TryParse(id, out Guid rideGuid))
                    {
                        return ApiResponseFactory.Error("Invalid ride ID format.", StatusCodes.Status400BadRequest);
                    }

                    // Build base query for rides
                    var rideQuery = db.Rides.AsQueryable();

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                    // If not a global admin, restrict rides to those within the user's associated companies.
                    if (!isGlobalAdmin)
                    {
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                        if (contactPerson == null)
                        {
                            return ApiResponseFactory.Error("No contact person profile found. You are not authorized.",
                                StatusCodes.Status403Forbidden);
                        }

                        // Retrieve all companies the contact person is associated with
                        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Distinct()
                            .ToList();

                        // Restrict rides to those whose CompanyId is in the user's associated companies
                        rideQuery = rideQuery.Where(r => associatedCompanyIds.Contains(r.CompanyId));
                    }

                    // Retrieve the ride details along with the Company and PartRides
                    var ride = await rideQuery
                        .Include(r => r.Company)
                        .Include(r => r.PartRides)
                        .ThenInclude(pr => pr.Car) // Include Car details
                        .Include(r => r.PartRides)
                        .ThenInclude(pr => pr.Driver).ThenInclude(driver => driver.User) // Include Driver details
                        .Include(r => r.PartRides)
                        .ThenInclude(pr => pr.Client) // Include Client details
                        .FirstOrDefaultAsync(r => r.Id == rideGuid);

                    if (ride == null)
                    {
                        return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                    }

                    var responseData = new
                    {
                        ride.Id,
                        ride.Name,
                        ride.Remark,
                        ride.CompanyId,
                        CompanyName = ride.Company.Name,
                        PartRides = ride.PartRides.Select(pr => new
                        {
                            pr.Id,
                            pr.Date,
                            pr.Start,
                            pr.End,
                            pr.Rest,
                            pr.TotalKilometers,
                            pr.Costs,
                            pr.WeekNumber,
                            pr.DecimalHours,
                            pr.CostsDescription,
                            pr.Turnover,
                            pr.Remark,
                            Car = pr.Car != null ? new { pr.Car.Id, pr.Car.LicensePlate } : null,
                            Driver = pr.Driver != null ? new { pr.Driver.Id, pr.Driver.AspNetUserId, pr.Driver?.User?.FirstName, pr.Driver?.User?.LastName  } : null,
                            Client = pr.Client != null ? new { pr.Client.Id, pr.Client.Name } : null
                        }).ToList()
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error retrieving ride details: {ex.Message}");
                    return ApiResponseFactory.Error("An unexpected error occurred while fetching ride details.",
                        StatusCodes.Status500InternalServerError);
                }
            });

        app.MapDelete("/rides/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                ITelegramNotificationService telegramService
            ) =>
            {
                try
                {
                    // Validate ride ID format
                    if (!Guid.TryParse(id, out Guid rideGuid))
                    {
                        return ApiResponseFactory.Error("Invalid ride ID format.", StatusCodes.Status400BadRequest);
                    }

                    var ride = await db.Rides
                        .Include(r => r.PartRides) // Check for related PartRides
                        .Include(r => r.DriverAssignments) // Include drivers for notification
                        .FirstOrDefaultAsync(r => r.Id == rideGuid);

                    if (ride == null)
                    {
                        return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                    }

                    // Authorization: Non-global admins must be part of the ride's company
                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                    if (!isGlobalAdmin)
                    {
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                        if (contactPerson == null)
                        {
                            return ApiResponseFactory.Error("No contact person profile found. You are not authorized.",
                                StatusCodes.Status403Forbidden);
                        }

                        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Distinct()
                            .ToList();

                        if (!associatedCompanyIds.Contains(ride.CompanyId))
                        {
                            return ApiResponseFactory.Error("You are not authorized to delete this ride.",
                                StatusCodes.Status403Forbidden);
                        }
                    }

                    // Prevent deletion if PartRides exist
                    if (ride.PartRides.Any())
                    {
                        return ApiResponseFactory.Error(
                            "Cannot delete the ride as it has associated PartRides.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Save driver IDs BEFORE deleting for notification
                    var assignedDriverIds = ride.DriverAssignments.Select(da => da.DriverId).ToList();
                    var isToday = ride.PlannedDate.HasValue && ride.PlannedDate.Value.Date == DateTime.UtcNow.Date;

                    // Delete the ride
                    db.Rides.Remove(ride);
                    await db.SaveChangesAsync();

                    // Send Telegram notification to all assigned drivers (awaited with error handling)
                    if (isToday && assignedDriverIds.Any())
                    {
                        try
                        {
                            await telegramService.NotifyDriversOnRideDeletedAsync(rideGuid, assignedDriverIds);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Telegram] Notification failed: {ex.Message}");
                        }
                    }

                    return ApiResponseFactory.Success("Ride deleted successfully.", StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error deleting ride: {ex.Message}");
                    return ApiResponseFactory.Error("An unexpected error occurred while deleting the ride.",
                        StatusCodes.Status500InternalServerError);
                }
            });
        
        return app;
    }
}