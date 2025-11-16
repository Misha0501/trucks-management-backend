using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class DailyPlanningEndpoints
    {
        public static void MapDailyPlanningEndpoints(this WebApplication app)
        {
            // GET /daily-planning/rides?date={date}&companyId={guid}
            app.MapGet("/daily-planning/rides",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] string date,
                    [FromQuery] string? companyId = null
                ) =>
                {
                    try
                    {
                        // Parse and validate date
                        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate))
                        {
                            return ApiResponseFactory.Error("Invalid date format. Use YYYY-MM-DD.", StatusCodes.Status400BadRequest);
                        }

                        targetDate = targetDate.Date.ToUniversalTime();

                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");

                        // Determine which companies the user can access
                        var allowedCompanyIds = new List<Guid>();

                        if (isGlobalAdmin)
                        {
                            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                allowedCompanyIds.Add(parsedCompanyId);
                            }
                            else
                            {
                                // Global admin without companyId filter - get all companies
                                allowedCompanyIds = await db.Companies.Select(c => c.Id).ToListAsync();
                            }
                        }
                        else
                        {
                            // Non-global admin: get companies via ContactPerson relationships
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            allowedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId!.Value)
                                .Distinct()
                                .ToList();

                            if (allowedCompanyIds.Count == 0)
                            {
                                return ApiResponseFactory.Error("No company access found.", StatusCodes.Status403Forbidden);
                            }

                            // If companyId is specified, verify access
                            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                if (!allowedCompanyIds.Contains(parsedCompanyId))
                                {
                                    return ApiResponseFactory.Error("Access denied to specified company.", StatusCodes.Status403Forbidden);
                                }
                                allowedCompanyIds = new List<Guid> { parsedCompanyId };
                            }
                        }

                        // Get all rides for the specific date that belong to allowed companies
                        var rides = await db.Rides
                            .Include(r => r.Client)
                            .Include(r => r.Truck)
                            .Include(r => r.DriverAssignments)
                                .ThenInclude(da => da.Driver)
                                    .ThenInclude(d => d.User)
                            .Where(r => r.PlannedDate.HasValue &&
                                       r.PlannedDate.Value.Date == targetDate.Date &&
                                       allowedCompanyIds.Contains(r.CompanyId))
                            .OrderBy(r => r.Client!.Name)
                            .ToListAsync();

                        // Group rides by client
                        var clientGroups = rides
                            .Where(r => r.Client != null && r.ClientId.HasValue)
                            .GroupBy(r => new { ClientId = r.ClientId!.Value, ClientName = r.Client!.Name })
                            .OrderBy(g => g.Key.ClientName)
                            .ToList();

                        var clientRidesData = new List<ClientDailyRidesDto>();
                        foreach (var clientGroup in clientGroups)
                        {
                            var clientRides = new List<RideAssignmentDto>();
                            foreach (var ride in clientGroup)
                            {
                                var primaryDriver = ride.DriverAssignments.FirstOrDefault(da => da.IsPrimary);
                                var secondDriver = ride.DriverAssignments.FirstOrDefault(da => !da.IsPrimary);

                                clientRides.Add(new RideAssignmentDto
                                {
                                    Id = ride.Id,
                                    TripNumber = ride.TripNumber,
                                    PlannedHours = ride.TotalPlannedHours,
                                    PlannedStartTime = ride.PlannedStartTime,
                                    PlannedEndTime = ride.PlannedEndTime,
                                    RouteFromName = ride.RouteFromName,
                                    RouteToName = ride.RouteToName,
                                    Notes = ride.Notes,
                                    CreationMethod = ride.CreationMethod,
                                    AssignedDriver = primaryDriver != null && primaryDriver.Driver?.User != null ? new DriverBasicDto
                                    {
                                        Id = primaryDriver.Driver.Id,
                                        FirstName = primaryDriver.Driver.User.FirstName,
                                        LastName = primaryDriver.Driver.User.LastName,
                                        PlannedHours = primaryDriver.PlannedHours
                                    } : null,
                                    SecondDriver = secondDriver != null && secondDriver.Driver?.User != null ? new DriverBasicDto
                                    {
                                        Id = secondDriver.Driver.Id,
                                        FirstName = secondDriver.Driver.User.FirstName,
                                        LastName = secondDriver.Driver.User.LastName,
                                        PlannedHours = secondDriver.PlannedHours
                                    } : null,
                                    AssignedTruck = ride.Truck != null ? new CarBasicDto
                                    {
                                        Id = ride.Truck.Id,
                                        LicensePlate = ride.Truck.LicensePlate
                                    } : null
                                });
                            }

                            clientRidesData.Add(new ClientDailyRidesDto
                            {
                                ClientId = clientGroup.Key.ClientId,
                                ClientName = clientGroup.Key.ClientName,
                                Rides = clientRides
                            });
                        }

                        var response = new DailyPlanningDto
                        {
                            Date = targetDate.ToString("yyyy-MM-dd"),
                            DayName = targetDate.ToString("dddd", CultureInfo.InvariantCulture),
                            Clients = clientRidesData
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving daily planning: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // GET /daily-planning/available-dates?companyId={guid}
            app.MapGet("/daily-planning/available-dates",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] string? companyId = null
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");

                        // Determine which companies the user can access
                        var allowedCompanyIds = new List<Guid>();

                        if (isGlobalAdmin)
                        {
                            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                allowedCompanyIds.Add(parsedCompanyId);
                            }
                            else
                            {
                                // Global admin without companyId filter - get all companies
                                allowedCompanyIds = await db.Companies.Select(c => c.Id).ToListAsync();
                            }
                        }
                        else
                        {
                            // Non-global admin: get companies via ContactPerson relationships
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            allowedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId!.Value)
                                .Distinct()
                                .ToList();

                            if (allowedCompanyIds.Count == 0)
                            {
                                return ApiResponseFactory.Error("No company access found.", StatusCodes.Status403Forbidden);
                            }

                            // If companyId is specified, verify access
                            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                if (!allowedCompanyIds.Contains(parsedCompanyId))
                                {
                                    return ApiResponseFactory.Error("Access denied to specified company.", StatusCodes.Status403Forbidden);
                                }
                                allowedCompanyIds = new List<Guid> { parsedCompanyId };
                            }
                        }

                        // Get distinct dates with rides (limit to reasonable range)
                        var now = DateTime.UtcNow.Date;
                        var dateRangeStart = now.AddDays(-30); // Last 30 days
                        var dateRangeEnd = now.AddDays(90);    // Next 90 days

                        var availableDates = await db.Rides
                            .Where(r => r.PlannedDate.HasValue &&
                                       r.PlannedDate.Value >= dateRangeStart &&
                                       r.PlannedDate.Value <= dateRangeEnd &&
                                       allowedCompanyIds.Contains(r.CompanyId))
                            .Select(r => r.PlannedDate!.Value.Date)
                            .Distinct()
                            .OrderBy(d => d)
                            .ToListAsync();

                        var dateStrings = availableDates
                            .Select(d => d.ToString("yyyy-MM-dd"))
                            .ToList();

                        var response = new AvailableDatesDto
                        {
                            Dates = dateStrings
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving available dates: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}

