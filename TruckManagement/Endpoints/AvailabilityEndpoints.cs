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
    public static class AvailabilityEndpoints
    {
        public static void MapAvailabilityEndpoints(this WebApplication app)
        {
            const decimal DEFAULT_HOURS = 8.0m;

            // GET /availability/week/{weekStartDate}?companyId={guid}
            app.MapGet("/availability/week/{weekStartDate}",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    string weekStartDate,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] string? companyId = null
                ) =>
                {
                    try
                    {
                        // Parse and validate date
                        if (!DateTime.TryParseExact(weekStartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var weekStart))
                        {
                            return ApiResponseFactory.Error("Invalid date format. Use YYYY-MM-DD.", StatusCodes.Status400BadRequest);
                        }

                        weekStart = weekStart.Date.ToUniversalTime();

                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");

                        // Determine allowed companies
                        var allowedCompanyIds = new List<Guid>();

                        if (isGlobalAdmin)
                        {
                            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                allowedCompanyIds.Add(parsedCompanyId);
                            }
                            else
                            {
                                allowedCompanyIds = await db.Companies.Select(c => c.Id).ToListAsync();
                            }
                        }
                        else
                        {
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

                            if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                if (!allowedCompanyIds.Contains(parsedCompanyId))
                                {
                                    return ApiResponseFactory.Error("Access denied to specified company.", StatusCodes.Status403Forbidden);
                                }
                                allowedCompanyIds = new List<Guid> { parsedCompanyId };
                            }
                        }

                        // Calculate week date range
                        var weekEnd = weekStart.AddDays(6);

                        // Get all drivers for allowed companies
                        var drivers = await db.Drivers
                            .Include(d => d.User)
                            .Where(d => allowedCompanyIds.Contains(d.CompanyId ?? Guid.Empty) && !d.IsDeleted)
                            .ToListAsync();

                        // Get all trucks for allowed companies
                        var trucks = await db.Cars
                            .Where(c => allowedCompanyIds.Contains(c.CompanyId))
                            .ToListAsync();

                        // Get custom availability records for the week
                        var driverAvailability = await db.DriverDailyAvailabilities
                            .Where(da => da.Date >= weekStart && da.Date <= weekEnd &&
                                        allowedCompanyIds.Contains(da.CompanyId))
                            .ToListAsync();

                        var truckAvailability = await db.TruckDailyAvailabilities
                            .Where(ta => ta.Date >= weekStart && ta.Date <= weekEnd &&
                                        allowedCompanyIds.Contains(ta.CompanyId))
                            .ToListAsync();

                        // Build driver availability data
                        var driverAvailabilityList = new List<DriverAvailabilityDto>();
                        foreach (var driver in drivers)
                        {
                            var availability = new Dictionary<string, DayAvailabilityDto>();
                            
                            for (int i = 0; i < 7; i++)
                            {
                                var date = weekStart.AddDays(i);
                                var dateString = date.ToString("yyyy-MM-dd");
                                
                                var customRecord = driverAvailability.FirstOrDefault(da => 
                                    da.DriverId == driver.Id && da.Date.Date == date.Date);

                                availability[dateString] = new DayAvailabilityDto
                                {
                                    Hours = customRecord?.AvailableHours ?? DEFAULT_HOURS,
                                    IsCustom = customRecord != null
                                };
                            }

                            driverAvailabilityList.Add(new DriverAvailabilityDto
                            {
                                DriverId = driver.Id,
                                FirstName = driver.User?.FirstName ?? "",
                                LastName = driver.User?.LastName ?? "",
                                Availability = availability
                            });
                        }

                        // Build truck availability data
                        var truckAvailabilityList = new List<TruckAvailabilityDto>();
                        foreach (var truck in trucks)
                        {
                            var availability = new Dictionary<string, DayAvailabilityDto>();
                            
                            for (int i = 0; i < 7; i++)
                            {
                                var date = weekStart.AddDays(i);
                                var dateString = date.ToString("yyyy-MM-dd");
                                
                                var customRecord = truckAvailability.FirstOrDefault(ta => 
                                    ta.TruckId == truck.Id && ta.Date.Date == date.Date);

                                availability[dateString] = new DayAvailabilityDto
                                {
                                    Hours = customRecord?.AvailableHours ?? DEFAULT_HOURS,
                                    IsCustom = customRecord != null
                                };
                            }

                            truckAvailabilityList.Add(new TruckAvailabilityDto
                            {
                                TruckId = truck.Id,
                                LicensePlate = truck.LicensePlate,
                                Availability = availability
                            });
                        }

                        var response = new WeeklyAvailabilityDto
                        {
                            WeekStartDate = weekStart.ToString("yyyy-MM-dd"),
                            Drivers = driverAvailabilityList,
                            Trucks = truckAvailabilityList
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error retrieving availability: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // PUT /availability/driver/{driverId}/bulk
            app.MapPut("/availability/driver/{driverId}/bulk",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid driverId,
                    [FromBody] BulkAvailabilityRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        // Get driver with company
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
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

                            if (!allowedCompanyIds.Contains(driver.CompanyId ?? Guid.Empty))
                            {
                                return ApiResponseFactory.Error("Access denied to this driver.", StatusCodes.Status403Forbidden);
                            }
                        }

                        var updatedDates = new List<UpdatedDateDto>();

                        foreach (var entry in request.Availability)
                        {
                            // Parse date
                            if (!DateTime.TryParseExact(entry.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                            {
                                continue; // Skip invalid dates
                            }

                            date = date.Date.ToUniversalTime();

                            // Validate hours range
                            if (entry.Value < 0 || entry.Value > 24)
                            {
                                return ApiResponseFactory.Error($"Invalid hours for {entry.Key}. Must be between 0 and 24.", StatusCodes.Status400BadRequest);
                            }

                            // Check if record exists
                            var existingRecord = await db.DriverDailyAvailabilities
                                .FirstOrDefaultAsync(da => da.DriverId == driverId && da.Date.Date == date.Date);

                            if (entry.Value == DEFAULT_HOURS)
                            {
                                // Delete record if it exists (revert to default)
                                if (existingRecord != null)
                                {
                                    db.DriverDailyAvailabilities.Remove(existingRecord);
                                }
                            }
                            else
                            {
                                // Create or update record
                                if (existingRecord != null)
                                {
                                    existingRecord.AvailableHours = entry.Value;
                                    existingRecord.UpdatedAt = DateTime.UtcNow;
                                }
                                else
                                {
                                    db.DriverDailyAvailabilities.Add(new DriverDailyAvailability
                                    {
                                        DriverId = driverId,
                                        Date = date,
                                        AvailableHours = entry.Value,
                                        CompanyId = driver.CompanyId ?? Guid.Empty
                                    });
                                }

                                updatedDates.Add(new UpdatedDateDto
                                {
                                    Date = entry.Key,
                                    Hours = entry.Value
                                });
                            }
                        }

                        await db.SaveChangesAsync();

                        var response = new BulkAvailabilityResponse
                        {
                            ResourceId = driverId,
                            UpdatedDates = updatedDates
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error updating driver availability: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // PUT /availability/truck/{truckId}/bulk
            app.MapPut("/availability/truck/{truckId}/bulk",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid truckId,
                    [FromBody] BulkAvailabilityRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        // Get truck with company
                        var truck = await db.Cars
                            .FirstOrDefaultAsync(c => c.Id == truckId);

                        if (truck == null)
                        {
                            return ApiResponseFactory.Error("Truck not found.", StatusCodes.Status404NotFound);
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

                            if (!allowedCompanyIds.Contains(truck.CompanyId))
                            {
                                return ApiResponseFactory.Error("Access denied to this truck.", StatusCodes.Status403Forbidden);
                            }
                        }

                        var updatedDates = new List<UpdatedDateDto>();

                        foreach (var entry in request.Availability)
                        {
                            // Parse date
                            if (!DateTime.TryParseExact(entry.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                            {
                                continue; // Skip invalid dates
                            }

                            date = date.Date.ToUniversalTime();

                            // Validate hours range
                            if (entry.Value < 0 || entry.Value > 24)
                            {
                                return ApiResponseFactory.Error($"Invalid hours for {entry.Key}. Must be between 0 and 24.", StatusCodes.Status400BadRequest);
                            }

                            // Check if record exists
                            var existingRecord = await db.TruckDailyAvailabilities
                                .FirstOrDefaultAsync(ta => ta.TruckId == truckId && ta.Date.Date == date.Date);

                            if (entry.Value == DEFAULT_HOURS)
                            {
                                // Delete record if it exists (revert to default)
                                if (existingRecord != null)
                                {
                                    db.TruckDailyAvailabilities.Remove(existingRecord);
                                }
                            }
                            else
                            {
                                // Create or update record
                                if (existingRecord != null)
                                {
                                    existingRecord.AvailableHours = entry.Value;
                                    existingRecord.UpdatedAt = DateTime.UtcNow;
                                }
                                else
                                {
                                    db.TruckDailyAvailabilities.Add(new TruckDailyAvailability
                                    {
                                        TruckId = truckId,
                                        Date = date,
                                        AvailableHours = entry.Value,
                                        CompanyId = truck.CompanyId
                                    });
                                }

                                updatedDates.Add(new UpdatedDateDto
                                {
                                    Date = entry.Key,
                                    Hours = entry.Value
                                });
                            }
                        }

                        await db.SaveChangesAsync();

                        var response = new BulkAvailabilityResponse
                        {
                            ResourceId = truckId,
                            UpdatedDates = updatedDates
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error updating truck availability: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}

