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
        }
    }
}

