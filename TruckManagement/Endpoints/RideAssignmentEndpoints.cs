using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Interfaces;

namespace TruckManagement.Endpoints
{
    public static class RideAssignmentEndpoints
    {
        public static void MapRideAssignmentEndpoints(this WebApplication app)
        {
            // PUT /rides/{id}/assign - Assign primary driver and truck
            app.MapPut("/rides/{id}/assign",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    [FromBody] AssignRideRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ITelegramNotificationService telegramService) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var ride = await db.Rides
                            .Include(r => r.DriverAssignments)
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Check access permissions
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");
                        
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id && 
                                                cpc.CompanyId == ride.CompanyId);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("Access denied to this ride.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Update total planned hours
                        ride.TotalPlannedHours = request.TotalPlannedHours;

                        // Handle driver assignment
                        var primaryDriverAssignment = ride.DriverAssignments.FirstOrDefault(da => da.IsPrimary);
                        
                        if (request.DriverId.HasValue)
                        {
                            // Verify driver exists
                            var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId.Value);
                            if (driver == null)
                            {
                                return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                            }

                            if (primaryDriverAssignment != null)
                            {
                                // Update existing assignment
                                primaryDriverAssignment.DriverId = request.DriverId.Value;
                                primaryDriverAssignment.PlannedHours = request.DriverPlannedHours ?? 8.0m;
                            }
                            else
                            {
                                // Create new assignment
                                var newAssignment = new RideDriverAssignment
                                {
                                    RideId = ride.Id,
                                    DriverId = request.DriverId.Value,
                                    PlannedHours = request.DriverPlannedHours ?? 8.0m,
                                    IsPrimary = true
                                };
                                db.RideDriverAssignments.Add(newAssignment);
                            }
                        }
                        else
                        {
                            // Unassign primary driver
                            if (primaryDriverAssignment != null)
                            {
                                db.RideDriverAssignments.Remove(primaryDriverAssignment);
                            }
                        }

                        // Handle truck assignment
                        if (request.TruckId.HasValue)
                        {
                            // Verify truck exists
                            var truck = await db.Cars.FirstOrDefaultAsync(c => c.Id == request.TruckId.Value);
                            if (truck == null)
                            {
                                return ApiResponseFactory.Error("Truck not found.", StatusCodes.Status404NotFound);
                            }
                            ride.TruckId = request.TruckId.Value;
                        }
                        else
                        {
                            ride.TruckId = null;
                        }

                        await db.SaveChangesAsync();

                        // Send Telegram notification (awaited with timeout protection)
                        if (request.DriverId.HasValue)
                        {
                            try
                            {
                                await telegramService.NotifyDriversOnRideAssignedAsync(
                                    id, 
                                    new List<Guid> { request.DriverId.Value });
                            }
                            catch (Exception ex)
                            {
                                // Log but don't fail the API call
                                Console.WriteLine($"[Telegram] Notification failed: {ex.Message}");
                            }
                        }

                        return ApiResponseFactory.Success("Ride assignment updated successfully.");
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error updating ride assignment: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // POST /rides/{id}/second-driver - Add second driver
            app.MapPost("/rides/{id}/second-driver",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    [FromBody] AddSecondDriverRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ITelegramNotificationService telegramService) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var ride = await db.Rides
                            .Include(r => r.DriverAssignments)
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Check access permissions
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");
                        
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id && 
                                                cpc.CompanyId == ride.CompanyId);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("Access denied to this ride.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Check if second driver already exists
                        var existingSecondDriver = ride.DriverAssignments.FirstOrDefault(da => !da.IsPrimary);
                        if (existingSecondDriver != null)
                        {
                            return ApiResponseFactory.Error("Second driver already assigned. Please delete existing second driver first.", StatusCodes.Status400BadRequest);
                        }

                        // Verify driver exists
                        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId);
                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }

                        // Create second driver assignment
                        var newAssignment = new RideDriverAssignment
                        {
                            RideId = ride.Id,
                            DriverId = request.DriverId,
                            PlannedHours = request.PlannedHours,
                            IsPrimary = false
                        };

                        db.RideDriverAssignments.Add(newAssignment);
                        await db.SaveChangesAsync();

                        // Get driver names for notifications
                        var secondDriver = await db.Drivers
                            .Include(d => d.User)
                            .FirstOrDefaultAsync(d => d.Id == request.DriverId);
                        
                        var secondDriverName = secondDriver != null 
                            ? $"{secondDriver.User?.FirstName} {secondDriver.User?.LastName}".Trim()
                            : "Onbekend";

                        var primaryAssignmentId = ride.DriverAssignments.FirstOrDefault(da => da.IsPrimary)?.DriverId;

                        // Notify both drivers (awaited with error handling)
                        try
                        {
                            // Notify in parallel for speed
                            var notificationTasks = new List<Task>
                            {
                                telegramService.NotifyDriversOnRideAssignedAsync(id, new List<Guid> { request.DriverId })
                            };

                            if (primaryAssignmentId.HasValue)
                            {
                                notificationTasks.Add(
                                    telegramService.NotifyDriverOnSecondDriverAddedAsync(id, primaryAssignmentId.Value, secondDriverName)
                                );
                            }

                            await Task.WhenAll(notificationTasks);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Telegram] Notification failed: {ex.Message}");
                        }

                        return ApiResponseFactory.Success("Second driver added successfully.", StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error adding second driver: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // DELETE /rides/{id}/second-driver - Remove second driver
            app.MapDelete("/rides/{id}/second-driver",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ITelegramNotificationService telegramService) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var ride = await db.Rides
                            .Include(r => r.DriverAssignments)
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Check access permissions
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");
                        
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id && 
                                                cpc.CompanyId == ride.CompanyId);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("Access denied to this ride.", StatusCodes.Status403Forbidden);
                            }
                        }

                        var secondDriver = ride.DriverAssignments.FirstOrDefault(da => !da.IsPrimary);
                        if (secondDriver == null)
                        {
                            return ApiResponseFactory.Error("No second driver assigned to this ride.", StatusCodes.Status404NotFound);
                        }

                        // Save info BEFORE deleting
                        var removedDriverId = secondDriver.DriverId;
                        var rideDetails = $"{ride.PlannedDate:dd-MM-yyyy} {ride.PlannedStartTime:hh\\:mm}";
                        var isToday = ride.PlannedDate.HasValue && ride.PlannedDate.Value.Date == DateTime.UtcNow.Date;

                        db.RideDriverAssignments.Remove(secondDriver);
                        await db.SaveChangesAsync();

                        // Notify removed driver (awaited with error handling)
                        if (isToday)
                        {
                            try
                            {
                                await telegramService.NotifyDriverOnRemovedFromRideAsync(removedDriverId, rideDetails);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Telegram] Notification failed: {ex.Message}");
                            }
                        }

                        return ApiResponseFactory.Success("Second driver removed successfully.");
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error removing second driver: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // PUT /rides/{id}/hours - Update planned hours
            app.MapPut("/rides/{id}/hours",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    [FromBody] UpdateRideHoursRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var ride = await db.Rides
                            .Include(r => r.DriverAssignments)
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Check access permissions
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");
                        
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id && 
                                                cpc.CompanyId == ride.CompanyId);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("Access denied to this ride.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Update total planned hours
                        ride.TotalPlannedHours = request.TotalPlannedHours;

                        // Update primary driver hours if specified
                        if (request.PrimaryDriverHours.HasValue)
                        {
                            var primaryDriver = ride.DriverAssignments.FirstOrDefault(da => da.IsPrimary);
                            if (primaryDriver != null)
                            {
                                primaryDriver.PlannedHours = request.PrimaryDriverHours.Value;
                            }
                        }

                        // Update second driver hours if specified
                        if (request.SecondDriverHours.HasValue)
                        {
                            var secondDriver = ride.DriverAssignments.FirstOrDefault(da => !da.IsPrimary);
                            if (secondDriver != null)
                            {
                                secondDriver.PlannedHours = request.SecondDriverHours.Value;
                            }
                        }

                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success("Ride hours updated successfully.");
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error updating ride hours: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // PUT /rides/{id}/details - Update ride details (routes and notes)
            app.MapPut("/rides/{id}/details",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    [FromBody] UpdateRideDetailsRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ITelegramNotificationService telegramService) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var ride = await db.Rides
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Check access permissions
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");
                        
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var hasAccess = await db.ContactPersonClientCompanies
                                .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id && 
                                                cpc.CompanyId == ride.CompanyId);

                            if (!hasAccess)
                            {
                                return ApiResponseFactory.Error("Access denied to this ride.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Track changes for notification
                        var changes = new List<string>();
                        if (ride.RouteFromName != (string.IsNullOrWhiteSpace(request.RouteFromName) ? null : request.RouteFromName.Trim()))
                            changes.Add($"Van locatie: {request.RouteFromName}");
                        if (ride.RouteToName != (string.IsNullOrWhiteSpace(request.RouteToName) ? null : request.RouteToName.Trim()))
                            changes.Add($"Naar locatie: {request.RouteToName}");
                        if (ride.PlannedStartTime != request.PlannedStartTime)
                            changes.Add($"Starttijd: {request.PlannedStartTime:hh\\:mm}");
                        if (ride.PlannedEndTime != request.PlannedEndTime)
                            changes.Add($"Eindtijd: {request.PlannedEndTime:hh\\:mm}");

                        // Update ride details (treat empty strings as null)
                        ride.RouteFromName = string.IsNullOrWhiteSpace(request.RouteFromName) ? null : request.RouteFromName.Trim();
                        ride.RouteToName = string.IsNullOrWhiteSpace(request.RouteToName) ? null : request.RouteToName.Trim();
                        ride.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
                        ride.PlannedStartTime = request.PlannedStartTime;
                        ride.PlannedEndTime = request.PlannedEndTime;

                        await db.SaveChangesAsync();

                        // Send Telegram notification if there were changes (awaited with error handling)
                        if (changes.Any())
                        {
                            try
                            {
                                var changesSummary = string.Join("\n", changes.Select(c => $"- {c}"));
                                await telegramService.NotifyDriversOnRideUpdatedAsync(id, changesSummary);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Telegram] Notification failed: {ex.Message}");
                            }
                        }

                        var response = new RideDetailsDto
                        {
                            Id = ride.Id,
                            RouteFromName = ride.RouteFromName,
                            RouteToName = ride.RouteToName,
                            Notes = ride.Notes,
                            PlannedStartTime = ride.PlannedStartTime,
                            PlannedEndTime = ride.PlannedEndTime,
                            UpdatedAt = DateTime.UtcNow
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error updating ride details: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // PUT /rides/{id}/trip-number - Update trip number
            app.MapPut("/rides/{id}/trip-number",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    [FromBody] UpdateTripNumberRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ITelegramNotificationService telegramService) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == id);

                        if (ride == null)
                        {
                            return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
                        }

                        // Check access permissions
                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");
                        
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            var allowedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId!.Value)
                                .ToList();

                            if (!allowedCompanyIds.Contains(ride.CompanyId))
                            {
                                return ApiResponseFactory.Error("Access denied to this ride.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // Track change for notification
                        var tripNumberChanged = ride.TripNumber != request.TripNumber;

                        // Update trip number
                        ride.TripNumber = request.TripNumber;
                        await db.SaveChangesAsync();

                        // Send Telegram notification if trip number changed (awaited with error handling)
                        if (tripNumberChanged)
                        {
                            try
                            {
                                var changesSummary = $"- Ritnummer: {request.TripNumber}";
                                await telegramService.NotifyDriversOnRideUpdatedAsync(id, changesSummary);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Telegram] Notification failed: {ex.Message}");
                            }
                        }

                        var response = new
                        {
                            Id = ride.Id,
                            TripNumber = ride.TripNumber
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error updating trip number: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}

