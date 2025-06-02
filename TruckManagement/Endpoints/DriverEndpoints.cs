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
    public static class DriversEndpoints
    {
        public static void MapDriversEndpoints(this WebApplication app)
        {
            app.MapGet("/drivers",
                [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
                async (
                    ApplicationDbContext db,
                    ClaimsPrincipal user,
                    UserManager<ApplicationUser> userManager,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
                ) =>
                {
                    // Validate pagination parameters
                    if (pageNumber < 1 || pageSize < 1)
                        return ApiResponseFactory.Error("Page number and page size must be greater than zero.",
                            StatusCodes.Status400BadRequest);

                    // Get the requesting user's ID and roles
                    var currentUserId = userManager.GetUserId(user);
                    var roles = await userManager.GetRolesAsync(await userManager.FindByIdAsync(currentUserId));

                    // Check if the user is a global admin
                    bool isGlobalAdmin = roles.Contains("globalAdmin");

                    // If the user is a global admin, retrieve all drivers
                    if (isGlobalAdmin)
                    {
                        var totalDrivers = await db.Drivers.CountAsync();
                        var drivers = await db.Drivers
                            .AsNoTracking()
                            .Include(d => d.Company)
                            .Include(d => d.User)
                            .OrderBy(d => d.User.Email)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(d => new
                            {
                                d.Id,
                                d.CompanyId,
                                CompanyName = d.Company != null ? d.Company.Name : null,
                                User = new
                                {
                                    d.User.Id,
                                    d.User.Email,
                                    d.User.FirstName,
                                    d.User.LastName
                                }
                            })
                            .ToListAsync();

                        return ApiResponseFactory.Success(new
                        {
                            TotalDrivers = totalDrivers,
                            PageNumber = pageNumber,
                            PageSize = pageSize,
                            Drivers = drivers
                        });
                    }

                    // For non-global admins, check if the user is a contact person
                    var contactPerson = await db.ContactPersons
                        .Include(cp => cp.ContactPersonClientCompanies)
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (contactPerson == null)
                        return ApiResponseFactory.Error("Unauthorized to access drivers.",
                            StatusCodes.Status403Forbidden);

                    // Retrieve associated company IDs for the contact person
                    var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                        .Where(cpc => cpc.CompanyId.HasValue)
                        .Select(cpc => cpc.CompanyId.Value)
                        .Distinct()
                        .ToList();

                    // Retrieve drivers for the associated companies
                    var totalAssociatedDrivers = await db.Drivers
                        .Where(d => associatedCompanyIds.Contains(d.CompanyId ?? Guid.Empty))
                        .CountAsync();

                    var associatedDrivers = await db.Drivers
                        .AsNoTracking()
                        .Include(d => d.Company)
                        .Include(d => d.User)
                        .Where(d => associatedCompanyIds.Contains(d.CompanyId ?? Guid.Empty))
                        .OrderBy(d => d.User.Email)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(d => new
                        {
                            d.Id,
                            d.CompanyId,
                            CompanyName = d.Company != null ? d.Company.Name : null,
                            User = new
                            {
                                d.User.Id,
                                d.User.Email,
                                d.User.FirstName,
                                d.User.LastName
                            }
                        })
                        .ToListAsync();

                    return ApiResponseFactory.Success(new
                    {
                        TotalDrivers = totalAssociatedDrivers,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        Drivers = associatedDrivers
                    });
                });

            app.MapGet("/drivers/period/current",
                [Authorize(Roles = "driver")] async (
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    var aspUserId = userManager.GetUserId(currentUser);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    var (year, periodNr, _) = DateHelper.GetPeriod(DateTime.UtcNow);
                    var (fromDate, toDate) = DateHelper.GetPeriodDateRange(year, periodNr);
                    
                    var approval = await db.PeriodApprovals
                        .Include(a => a.PartRides)
                        .FirstOrDefaultAsync(a => a.DriverId == driver.Id &&
                                                  a.Year == year &&
                                                  a.PeriodNr == periodNr);

                    if (approval == null)
                    {
                        return ApiResponseFactory.Success(new
                        {
                            Year = year,
                            PeriodNr = periodNr,
                            Status = "NoApproval",
                            Weeks = Enumerable.Range(1, 4).Select(i => new
                            {
                                WeekInPeriod = i,
                                WeekNumber = DateHelper.GetWeekNumberOfPeriod(year, periodNr, i),
                                PartRides = new List<object>()
                            }).OrderByDescending(w => w.WeekNumber) 
                        });
                    }

                    var groupedWeeks = Enumerable.Range(1, 4).Select(i =>
                        {
                            int weekNumber = DateHelper.GetWeekNumberOfPeriod(year, periodNr, i);
                            var partRidesForWeek = approval.PartRides
                                .Where(r => r.WeekNrInPeriod == i)
                                .OrderByDescending(r => r.Date)
                                .Select(r => new
                                {
                                    r.Id,
                                    r.Date,
                                    r.Start,
                                    r.End,
                                    r.Kilometers,
                                    r.DecimalHours,
                                    r.Remark
                                })
                                .ToList();

                            return new
                            {
                                WeekInPeriod = i,
                                WeekNumber = weekNumber,
                                PartRides = partRidesForWeek
                            };
                        })
                        .OrderByDescending(g => g.WeekInPeriod)
                        .ToList();

                    return ApiResponseFactory.Success(new
                    {
                        approval.Year,
                        approval.PeriodNr,
                        approval.Status,
                        fromDate,
                        toDate,
                        Weeks = groupedWeeks
                    });
                });

            app.MapGet("/drivers/period/pending",
                [Authorize(Roles = "driver")] async (
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    var aspUserId = userManager.GetUserId(currentUser);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    var pending = await db.PeriodApprovals
                        .Where(a => a.DriverId == driver.Id &&
                                    (a.Status == PeriodApprovalStatus.PendingDriver ||
                                     a.Status == PeriodApprovalStatus.PendingAdmin ||
                                     a.Status == PeriodApprovalStatus.Invalidated))
                        .OrderBy(a => a.Year)
                        .ThenBy(a => a.PeriodNr)
                        .Select(a => new
                        {
                            a.Year,
                            a.PeriodNr,
                            a.Status,
                            RideCount = a.PartRides.Count
                        })
                        .ToListAsync();

                    return ApiResponseFactory.Success(pending);
                });

            app.MapGet("/drivers/period/archived",
                [Authorize(Roles = "driver")] async (
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    var aspUserId = userManager.GetUserId(currentUser);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    var archived = await db.PeriodApprovals
                        .Where(a => a.DriverId == driver.Id &&
                                    a.Status == PeriodApprovalStatus.Signed)
                        .OrderByDescending(a => a.Year)
                        .ThenByDescending(a => a.PeriodNr)
                        .Select(a => new
                        {
                            a.Year,
                            a.PeriodNr,
                            a.Status,
                            RideCount = a.PartRides.Count
                        })
                        .ToListAsync();

                    return ApiResponseFactory.Success(archived);
                });
        }
    }
}