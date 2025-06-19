using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Services;

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

            app.MapGet("/drivers/periods/current",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
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
                            fromDate,
                            toDate,
                            Weeks = Enumerable.Range(1, 4).Select(i => new
                            {
                                WeekInPeriod = i,
                                WeekNumber = DateHelper.GetWeekNumberOfPeriod(year, periodNr, i),
                                TotalDecimalHours = 0.0,
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

                            double totalDecimalHours = partRidesForWeek
                                .Sum(r => r.DecimalHours ?? 0);

                            return new
                            {
                                WeekInPeriod = i,
                                WeekNumber = weekNumber,
                                TotalDecimalHours = Math.Round(totalDecimalHours, 2),
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

            app.MapGet("/drivers/periods/pending",
                [Authorize(Roles = "driver,globalAdmin")] async (
                    UserManager<ApplicationUser> userMgr,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize   = 10) =>
                {
                    var aspUserId = userMgr.GetUserId(currentUser);
                    var driver    = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    /* Weeks that are *not* signed yet */
                    var weeks = WeekApprovalQueryHelper.FilterWeeks(db.WeekApprovals.AsNoTracking(),
                            driver.Id, null)
                        .Where(w => w.Status == WeekApprovalStatus.PendingAdmin
                                    || w.Status == WeekApprovalStatus.PendingDriver);

                    /* Group by period */
                    var grouped = weeks
                        .GroupBy(w => new { w.Year, w.PeriodNr })
                        .Select(g => new
                        {
                            g.Key.Year,
                            g.Key.PeriodNr,
                            Status = g.Any(w => w.Status == WeekApprovalStatus.PendingAdmin)
                                ? WeekApprovalStatus.PendingAdmin
                                : WeekApprovalStatus.PendingDriver,
                            FromDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).fromDate,
                            ToDate   = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).toDate
                        });

                    var totalCount = await grouped.CountAsync();
                    var data = await grouped
                        .OrderByDescending(x => x.Year)
                        .ThenByDescending(x => x.PeriodNr)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    return ApiResponseFactory.Success(new
                    {
                        pageNumber,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        data
                    });
                });

            app.MapGet("/drivers/periods/archived",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10) =>
                {
                    var aspUserId = userManager.GetUserId(currentUser);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    var (currentYear, currentPeriodNr, _) = DateHelper.GetPeriod(DateTime.UtcNow);

                    var query = db.PeriodApprovals
                        .Where(a =>
                            a.DriverId == driver.Id &&
                            a.Status == PeriodApprovalStatus.Signed &&
                            !(a.Year == currentYear && a.PeriodNr == currentPeriodNr));

                    var totalCount = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    var rawApprovals = await query
                        .OrderByDescending(a => a.Year)
                        .ThenByDescending(a => a.PeriodNr)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

                    var archived = rawApprovals
                        .Select(a =>
                        {
                            var (fromDate, toDate) = DateHelper.GetPeriodDateRange(a.Year, a.PeriodNr);
                            return new
                            {
                                a.Year,
                                a.PeriodNr,
                                a.Status,
                                fromDate,
                                toDate
                            };
                        })
                        .ToList();

                    return ApiResponseFactory.Success(new
                    {
                        pageNumber,
                        pageSize,
                        totalCount,
                        totalPages,
                        data = archived
                    });
                });

            app.MapGet("/drivers/periods/{periodKey}",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
                    string periodKey,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    var aspUserId = userManager.GetUserId(currentUser);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Parse format "YYYY-P"
                    var parts = periodKey.Split('-');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out var year) ||
                        !int.TryParse(parts[1], out var periodNr))
                    {
                        return ApiResponseFactory.Error("Invalid period key format. Use 'YYYY-P' (e.g., 2024-6).",
                            StatusCodes.Status400BadRequest);
                    }

                    var (fromDate, toDate) = DateHelper.GetPeriodDateRange(year, periodNr);

                    var approval = await db.PeriodApprovals
                        .Include(a => a.PartRides)
                        .FirstOrDefaultAsync(a => a.DriverId == driver.Id &&
                                                  a.Year == year &&
                                                  a.PeriodNr == periodNr);

                    if (approval == null)
                    {
                        return ApiResponseFactory.Error("Period not found.", StatusCodes.Status404NotFound);
                    }

                    double totalDecimalHours = 0.0;
                    decimal totalEarnings = 0.0m;

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
                                    r.TaxFreeCompensation,
                                    r.VariousCompensation,
                                    r.Remark
                                })
                                .ToList();

                            double weekHours = partRidesForWeek.Sum(r => r.DecimalHours ?? 0);
                            decimal weekEarnings = partRidesForWeek.Sum(r =>
                                (decimal)(r.TaxFreeCompensation + r.VariousCompensation));

                            totalDecimalHours += weekHours;
                            totalEarnings += weekEarnings;

                            return new
                            {
                                WeekInPeriod = i,
                                WeekNumber = weekNumber,
                                TotalDecimalHours = Math.Round(weekHours, 2),
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
                        TotalDecimalHours = Math.Round(totalDecimalHours, 2),
                        TotalEarnings = Math.Round(totalEarnings, 2),
                        Weeks = groupedWeeks
                    });
                });
        }
    }
}