using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class WeeklyPlanningEndpoints
    {
        public static void MapWeeklyPlanningEndpoints(this WebApplication app)
        {
            // GET /weekly-planning/preview?weekStartDate={date}&companyId={guid}
            app.MapGet("/weekly-planning/preview",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] string weekStartDate,
                    [FromQuery] string? companyId = null
                ) =>
                {
                    try
                    {
                        // Parse and validate week start date
                        if (!DateTime.TryParse(weekStartDate, out var weekStart))
                        {
                            return ApiResponseFactory.Error("Invalid date format. Use YYYY-MM-DD.", StatusCodes.Status400BadRequest);
                        }

                        // Ensure it's a Monday
                        if (weekStart.DayOfWeek != DayOfWeek.Monday)
                        {
                            // Adjust to the Monday of that week
                            int daysToSubtract = ((int)weekStart.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                            weekStart = weekStart.AddDays(-daysToSubtract);
                        }

                        weekStart = DateTime.SpecifyKind(weekStart.Date, DateTimeKind.Utc);

                        // Get current user
                        var currentUserId = userManager.GetUserId(currentUser);
                        var isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Determine company filter
                        Guid? filterCompanyId = null;
                        if (!string.IsNullOrEmpty(companyId))
                        {
                            if (!Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                return ApiResponseFactory.Error("Invalid company ID format.", StatusCodes.Status400BadRequest);
                            }
                            filterCompanyId = parsedCompanyId;
                        }

                        // Check authorization and get allowed companies
                        List<Guid> allowedCompanyIds = new();
                        
                        if (isGlobalAdmin)
                        {
                            if (filterCompanyId.HasValue)
                            {
                                allowedCompanyIds.Add(filterCompanyId.Value);
                            }
                            else
                            {
                                // Global admin without filter - get all companies
                                allowedCompanyIds = await db.Companies.Select(c => c.Id).ToListAsync();
                            }
                        }
                        else
                        {
                            // Non-global admin - get their companies
                            var contactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                            }

                            allowedCompanyIds = await db.ContactPersonClientCompanies
                                .Where(cpc => cpc.ContactPersonId == contactPerson.Id && cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId!.Value)
                                .Distinct()
                                .ToListAsync();

                            // If company filter specified, ensure they have access
                            if (filterCompanyId.HasValue && !allowedCompanyIds.Contains(filterCompanyId.Value))
                            {
                                return ApiResponseFactory.Error("You don't have access to this company.", StatusCodes.Status403Forbidden);
                            }

                            // If filter specified, use only that company
                            if (filterCompanyId.HasValue)
                            {
                                allowedCompanyIds = new List<Guid> { filterCompanyId.Value };
                            }
                        }

                        // Calculate week end date (Sunday)
                        var weekEnd = weekStart.AddDays(6);

                        // Get all active capacity templates that overlap with this week
                        var templates = await db.ClientCapacityTemplates
                            .Include(t => t.Client)
                            .Where(t => allowedCompanyIds.Contains(t.CompanyId)
                                     && t.IsActive
                                     && t.StartDate <= weekEnd
                                     && t.EndDate >= weekStart)
                            .ToListAsync();

                        // Build preview for each day of the week
                        var days = new List<DayPreviewDto>();

                        for (int i = 0; i < 7; i++)
                        {
                            var currentDate = weekStart.AddDays(i);
                            var dayOfWeek = currentDate.DayOfWeek;

                            // Group by client and aggregate truck needs
                            var clientAggregates = new Dictionary<Guid, (string clientName, int trucks, List<Guid> templates)>();

                            foreach (var template in templates)
                            {
                                // Check if template is active for this date
                                if (currentDate >= template.StartDate && currentDate <= template.EndDate)
                                {
                                    int trucksForDay = dayOfWeek switch
                                    {
                                        DayOfWeek.Monday => template.MondayTrucks,
                                        DayOfWeek.Tuesday => template.TuesdayTrucks,
                                        DayOfWeek.Wednesday => template.WednesdayTrucks,
                                        DayOfWeek.Thursday => template.ThursdayTrucks,
                                        DayOfWeek.Friday => template.FridayTrucks,
                                        DayOfWeek.Saturday => template.SaturdayTrucks,
                                        DayOfWeek.Sunday => template.SundayTrucks,
                                        _ => 0
                                    };

                                    if (trucksForDay > 0)
                                    {
                                        if (clientAggregates.ContainsKey(template.ClientId))
                                        {
                                            var existing = clientAggregates[template.ClientId];
                                            existing.trucks += trucksForDay;
                                            existing.templates.Add(template.Id);
                                            clientAggregates[template.ClientId] = existing;
                                        }
                                        else
                                        {
                                            clientAggregates[template.ClientId] = (
                                                template.Client.Name,
                                                trucksForDay,
                                                new List<Guid> { template.Id }
                                            );
                                        }
                                    }
                                }
                            }

                            // Build client list for this day
                            var clients = clientAggregates
                                .Select(kvp => new ClientDayPreviewDto
                                {
                                    ClientId = kvp.Key,
                                    ClientName = kvp.Value.clientName,
                                    TrucksNeeded = kvp.Value.trucks,
                                    SourceTemplates = kvp.Value.templates
                                })
                                .OrderBy(c => c.ClientName)
                                .ToList();

                            days.Add(new DayPreviewDto
                            {
                                Date = currentDate.ToString("yyyy-MM-dd"),
                                DayName = currentDate.ToString("dddd", CultureInfo.InvariantCulture),
                                Clients = clients
                            });
                        }

                        var preview = new WeeklyPlanningPreviewDto
                        {
                            WeekStartDate = weekStart.ToString("yyyy-MM-dd"),
                            Days = days
                        };

                        return ApiResponseFactory.Success(preview);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error generating preview: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });

            // POST /weekly-planning/generate
            app.MapPost("/weekly-planning/generate",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    [FromBody] GenerateRidesRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var user = await userManager.FindByIdAsync(userId!);
                        if (user == null) return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                        var userRoles = await userManager.GetRolesAsync(user);
                        var isGlobalAdmin = userRoles.Contains("globalAdmin");

                        // Normalize week start date
                        var weekStart = request.WeekStartDate.ToUniversalTime().Date;
                        if (weekStart.DayOfWeek != DayOfWeek.Monday)
                        {
                            int daysToSubtract = ((int)weekStart.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                            weekStart = weekStart.AddDays(-daysToSubtract);
                        }

                        // Validation
                        if (request.Days == null || request.Days.Count == 0)
                        {
                            return ApiResponseFactory.Error("No days provided for ride generation.", StatusCodes.Status400BadRequest);
                        }

                        var response = new GenerateRidesResponse
                        {
                            WeekStartDate = weekStart
                        };

                        int totalRidesGenerated = 0;

                        // Process each day
                        foreach (var day in request.Days)
                        {
                            var dayDate = day.Date.ToUniversalTime().Date;
                            var dayResult = new DayRideResultDto
                            {
                                Date = dayDate
                            };

                            // Process each client for this day
                            foreach (var clientRequest in day.Clients)
                            {
                                // Verify client exists and user has access
                                var client = await db.Clients
                                    .FirstOrDefaultAsync(c => c.Id == clientRequest.ClientId);

                                if (client == null)
                                {
                                    return ApiResponseFactory.Error($"Client {clientRequest.ClientId} not found.", StatusCodes.Status404NotFound);
                                }

                                // Check access permissions
                                if (!isGlobalAdmin)
                                {
                                    // Check if user has access to this client's company
                                    var contactPerson = await db.ContactPersons
                                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                                    if (contactPerson == null)
                                    {
                                        return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                                    }

                                    var hasAccess = await db.ContactPersonClientCompanies
                                        .AnyAsync(cpc => cpc.ContactPersonId == contactPerson.Id && 
                                                        cpc.CompanyId == client.CompanyId);

                                    if (!hasAccess)
                                    {
                                        return ApiResponseFactory.Error($"Access denied to client {client.Name}.", StatusCodes.Status403Forbidden);
                                    }
                                }

                                // Validate trucks to generate
                                if (clientRequest.TrucksToGenerate < 0)
                                {
                                    return ApiResponseFactory.Error($"Invalid number of trucks for client {client.Name}.", StatusCodes.Status400BadRequest);
                                }

                                if (clientRequest.TrucksToGenerate == 0)
                                {
                                    // Skip this client if no trucks requested
                                    continue;
                                }

                                // Generate rides for this client on this day
                                var rideIds = new List<Guid>();
                                for (int i = 0; i < clientRequest.TrucksToGenerate; i++)
                                {
                                    var ride = new Ride
                                    {
                                        Id = Guid.NewGuid(),
                                        CompanyId = client.CompanyId,
                                        ClientId = client.Id,
                                        PlannedDate = dayDate,
                                        PlannedHours = 8.0m,
                                        CreationMethod = "TEMPLATE_GENERATED",
                                        CreatedAt = DateTime.UtcNow,
                                        RouteFromName = null,
                                        RouteToName = null,
                                        Notes = null
                                    };

                                    db.Rides.Add(ride);
                                    rideIds.Add(ride.Id);
                                    totalRidesGenerated++;
                                }

                                // Add to day result
                                dayResult.Clients.Add(new ClientRideResultDto
                                {
                                    ClientId = client.Id,
                                    ClientName = client.Name,
                                    RidesGenerated = rideIds.Count,
                                    RideIds = rideIds
                                });
                            }

                            response.Days.Add(dayResult);
                        }

                        // Save all rides to database
                        await db.SaveChangesAsync();

                        response.TotalRidesGenerated = totalRidesGenerated;

                        return ApiResponseFactory.Success(response, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error($"Error generating rides: {ex.Message}", StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}

