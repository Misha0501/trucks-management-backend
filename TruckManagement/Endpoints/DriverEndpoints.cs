using System.Globalization;
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
                    try
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
                                .Include(d => d.Car)
                                .Include(d => d.User)
                                .OrderBy(d => d.User.Email)
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .Select(d => new
                                {
                                    d.Id,
                                    d.CompanyId,
                                    CompanyName = d.Company != null ? d.Company.Name : null,
                                    d.CarId,
                                    CarLicensePlate = d.Car != null ? d.Car.LicensePlate : null,
                                    CarVehicleYear = d.Car != null ? d.Car.VehicleYear : null,
                                    CarRegistrationDate = d.Car != null ? d.Car.RegistrationDate : null,
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
                            .Include(d => d.Car)
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
                                d.CarId,
                                CarLicensePlate = d.Car != null ? d.Car.LicensePlate : null,
                                CarVehicleYear = d.Car != null ? d.Car.VehicleYear : null,
                                CarRegistrationDate = d.Car != null ? d.Car.RegistrationDate : null,
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
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/periods/current",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    try
                    {
                        // -------------------------------------------------------------------
                        // 1. Resolve driver
                        // -------------------------------------------------------------------
                        var aspUserId = userManager.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver == null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

                        // -------------------------------------------------------------------
                        // 2. Determine current period
                        // -------------------------------------------------------------------
                        var (year, periodNr, _) = DateHelper.GetPeriod(DateTime.UtcNow);
                        var (fromDate, toDate) = DateHelper.GetPeriodDateRange(year, periodNr);

                        // -------------------------------------------------------------------
                        // 3. Fetch WeekApprovals for that period
                        // -------------------------------------------------------------------
                        var weeksQuery = db.WeekApprovals
                            .AsNoTracking()
                            .Include(w => w.PartRides)
                            .Where(w => w.DriverId == driver.Id &&
                                        w.Year == year &&
                                        w.PeriodNr == periodNr);

                        var weekApprovals = await weeksQuery.ToListAsync();

                        // -------------------------------------------------------------------
                        // 4. Build four-week payload (fill empty weeks if needed)
                        // -------------------------------------------------------------------
                        var weeks = Enumerable.Range(1, 4).Select(weekInPeriod =>
                            {
                                int weekNumber = DateHelper.GetWeekNumberOfPeriod(year, periodNr, weekInPeriod);
                                var wa = weekApprovals.FirstOrDefault(w => w.WeekNr == weekNumber);

                                // If week has no WeekApproval record yet, create an empty shell
                                var rides = wa?.PartRides
                                    .OrderByDescending(r => r.Date)
                                    .Select(r => new
                                    {
                                        r.Id,
                                        r.Date,
                                        r.Start,
                                        r.End,
                                        r.TotalKilometers,
                                        r.DecimalHours,
                                        r.Remark,
                                        Status = r.Status
                                    })
                                    .Cast<dynamic>()
                                    .ToList() ?? new List<dynamic>();

                                double totalDecimalHours = rides.Sum(r =>
                                {
                                    var dhProp = r.GetType().GetProperty("DecimalHours")!;
                                    return (double?)dhProp.GetValue(r)! ?? 0;
                                });

                                bool isCurrentWeek = DateHelper.GetIso8601WeekOfYear(DateTime.UtcNow) == weekNumber &&
                                                     DateTime.UtcNow.Year == year;

                                WeekApprovalStatus? status;
                                if (wa?.Status != null)
                                {
                                    status = wa.Status;
                                }
                                else if (isCurrentWeek)
                                {
                                    status = WeekApprovalStatus.PendingAdmin;
                                }
                                else if (wa != null && wa.PartRides.Any())
                                {
                                    status = wa.Status;
                                }
                                else
                                {
                                    status = null;
                                }

                                return new
                                {
                                    WeekInPeriod = weekInPeriod,
                                    WeekNumber = weekNumber,
                                    Status = status,
                                    TotalDecimalHours = Math.Round(totalDecimalHours, 2),
                                    PartRides = rides
                                };
                            })
                            .OrderByDescending(w => w.WeekInPeriod) // newest week first
                            .ToList();

                        // -------------------------------------------------------------------
                        // 5. Return
                        // -------------------------------------------------------------------
                        return ApiResponseFactory.Success(new
                        {
                            Year = year,
                            PeriodNr = periodNr,
                            FromDate = fromDate,
                            ToDate = toDate,
                            Weeks = weeks
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/periods/pending",
                [Authorize(Roles = "driver,globalAdmin")]
                async (
                    UserManager<ApplicationUser> userMgr,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10) =>
                {
                    try
                    {
                        var aspUserId = userMgr.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver == null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

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
                                ToDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).toDate
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
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
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
                    try
                    {
                        // ------------------------------------------------- 1. Resolve driver
                        var aspUserId = userManager.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver is null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

                        // ------------------------------------------------- 2. Current period (to exclude)
                        var (currYear, currPeriodNr, _) = DateHelper.GetPeriod(DateTime.UtcNow);

                        // ------------------------------------------------- 3. Query WeekApprovals
                        var weeks = db.WeekApprovals.AsNoTracking()
                            .Where(w => w.DriverId == driver.Id
                                        && w.Status == WeekApprovalStatus.Signed
                                        // keep only periods strictly *before* the current one
                                        && ((w.Year < currYear) ||
                                            (w.Year == currYear && w.PeriodNr < currPeriodNr)));

                        // ------------------------------------------------- 4. Group by period and keep only FULLY-signed ones
                        var periodsQuery = weeks
                            .GroupBy(w => new { w.Year, w.PeriodNr })
                            .Where(g => g.All(w => w.Status == WeekApprovalStatus.Signed))
                            .Select(g => new
                            {
                                g.Key.Year,
                                g.Key.PeriodNr,
                                Status = WeekApprovalStatus.Signed,
                                // helper to compute date range
                                FromDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).fromDate,
                                ToDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).toDate
                            });

                        // ------------------------------------------------- 5. Paging
                        var totalCount = await periodsQuery.CountAsync();
                        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                        var data = await periodsQuery
                            .OrderByDescending(p => p.Year)
                            .ThenByDescending(p => p.PeriodNr)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

                        // ------------------------------------------------- 6. Return
                        return ApiResponseFactory.Success(new
                        {
                            pageNumber,
                            pageSize,
                            totalCount,
                            totalPages,
                            data
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            // ---------------------------------------------------------------------------
            //  PERIOD ♦ DETAIL  – built from WeekApprovals
            //  GET /drivers/periods/{periodKey}   (periodKey = "YYYY-P")
            // ---------------------------------------------------------------------------
            app.MapGet("/drivers/periods/{periodKey}",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
                    string periodKey,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    try
                    {
                        // 1) Resolve driver
                        var aspUserId = userManager.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver is null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

                        // 2) Parse periodKey => year‑periodNr
                        var parts = periodKey.Split('-');
                        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) ||
                            !int.TryParse(parts[1], out var periodNr))
                            return ApiResponseFactory.Error("Invalid period key format. Use 'YYYY-P' (e.g., 2024-6).",
                                StatusCodes.Status400BadRequest);

                        var (fromDate, toDate) = DateHelper.GetPeriodDateRange(year, periodNr);

                        // 3) Load WeekApprovals for driver/period
                        var waList = await db.WeekApprovals
                            .AsNoTracking()
                            .Include(w => w.PartRides)
                            .Where(w => w.DriverId == driver.Id &&
                                        w.Year == year &&
                                        w.PeriodNr == periodNr)
                            .ToListAsync();

                        if (!waList.Any())
                            return ApiResponseFactory.Error("Period not found.", StatusCodes.Status404NotFound);

                        // 4) Build week buckets (ensure 4 weeks even if some are missing)
                        double totalDecimalHours = 0;
                        decimal totalEarnings = 0;

                        var weeks = Enumerable.Range(1, 4).Select(weekInPeriod =>
                            {
                                int weekNumber = DateHelper.GetWeekNumberOfPeriod(year, periodNr, weekInPeriod);
                                var wa = waList.FirstOrDefault(w => w.PartRides.Any(pr => pr.WeekNumber == weekNumber));

                                var rides = wa?.PartRides
                                    .OrderByDescending(r => r.Date)
                                    .Select(r => new
                                    {
                                        r.Id,
                                        r.Date,
                                        r.Start,
                                        r.End,
                                        r.TotalKilometers,
                                        r.DecimalHours,
                                        r.TaxFreeCompensation,
                                        r.VariousCompensation,
                                        r.Remark,
                                        r.Status
                                    })
                                    .Cast<dynamic>()
                                    .ToList() ?? new List<dynamic>();

                                // totals
                                double weekHours = rides.Sum(r => (double?)(r.DecimalHours ?? 0) ?? 0);
                                decimal weekEarnings = rides.Sum(r =>
                                {
                                    decimal tfc = (decimal)(r.TaxFreeCompensation ?? 0);
                                    decimal vc = (decimal)(r.VariousCompensation ?? 0);
                                    return tfc + vc;
                                });

                                totalDecimalHours += weekHours;
                                totalEarnings += weekEarnings;

                                bool isCurrentWeek = DateHelper.GetIso8601WeekOfYear(DateTime.UtcNow) == weekNumber &&
                                                     DateTime.UtcNow.Year == year;

                                return new
                                {
                                    WeekInPeriod = weekInPeriod,
                                    WeekNumber = weekNumber,
                                    Status = isCurrentWeek
                                        ? (wa?.Status ?? WeekApprovalStatus.PendingAdmin)
                                        : wa?.Status,
                                    TotalDecimalHours = Math.Round(weekHours, 2),
                                    PartRides = rides
                                };
                            })
                            .OrderByDescending(w => w.WeekInPeriod)
                            .ToList();

                        return ApiResponseFactory.Success(new
                        {
                            Year = year,
                            PeriodNr = periodNr,
                            FromDate = fromDate,
                            ToDate = toDate,
                            TotalDecimalHours = Math.Round(totalDecimalHours, 2),
                            TotalEarnings = Math.Round(totalEarnings, 2),
                            Weeks = weeks
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/week/details",
                [Authorize(Roles = "driver")] async (
                    [FromQuery] int? year,
                    [FromQuery] int? weekNumber,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db
                ) =>
                {
                    try
                    {
                        if (!year.HasValue || !weekNumber.HasValue || year <= 0 || weekNumber <= 0)
                        {
                            return ApiResponseFactory.Error(
                                "Query parameters 'year' and 'weekNumber' are required and must be greater than zero.",
                                StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        var driver =
                            await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);
                        }

                        var query = db.WeekApprovals
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Car)
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Client)
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Company)
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Driver)
                            .ThenInclude(d => d.User)
                            .Where(wa => wa.Year == year.Value && wa.WeekNr == weekNumber.Value)
                            .Where(wa =>
                                wa.Status == WeekApprovalStatus.Signed ||
                                wa.Status == WeekApprovalStatus.PendingDriver);

                        query = query.Where(wa => wa.DriverId == driver.Id);

                        var weekApproval = await query.FirstOrDefaultAsync();

                        if (weekApproval == null)
                        {
                            return ApiResponseFactory.Error("Week not found or not accessible.",
                                StatusCodes.Status404NotFound);
                        }

                        DateTime weekStart = ISOWeek.ToDateTime(year.Value, weekNumber.Value, DayOfWeek.Monday);
                        DateTime weekEnd = weekStart.AddDays(6);

                        var rides = weekApproval.PartRides
                            .Select(pr => new
                            {
                                pr.Id,
                                pr.Date,
                                pr.Start,
                                pr.End,
                                pr.TotalKilometers,
                                pr.DecimalHours,
                                pr.Remark,
                                Car = pr.Car != null ? new { pr.Car.Id, pr.Car.LicensePlate } : null,
                                Client = pr.Client != null ? new { pr.Client.Id, pr.Client.Name } : null,
                                Company = pr.Company != null ? new { pr.Company.Id, pr.Company.Name } : null
                            }).ToList();

                        double totalCompensation = weekApproval.PartRides.Sum(pr =>
                            pr.TaxFreeCompensation + pr.NightAllowance + pr.KilometerReimbursement + pr.ConsignmentFee +
                            pr.VariousCompensation);

                        // TODO: Update vacation hours 
                        return ApiResponseFactory.Success(new
                        {
                            weekApprovalId = weekApproval.Id,
                            week = weekNumber.Value,
                            year = year.Value,
                            startDate = weekStart,
                            endDate = weekEnd,
                            status = weekApproval.Status,
                            vacationHoursLeft = 0,
                            vacationHoursTaken = 0,
                            totalCompensation = Math.Round(totalCompensation, 2),
                            totalHoursWorked = Math.Round(weekApproval.PartRides.Sum(pr => pr.DecimalHours ?? 0), 2),
                            rides
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapPost("/drivers/week/sign",
                [Authorize(Roles = "driver")] async (
                    [FromQuery] int year,
                    [FromQuery] int weekNumber,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db
                ) =>
                {
                    try
                    {
                        if (year <= 0 || weekNumber <= 0)
                        {
                            return ApiResponseFactory.Error(
                                "Query parameters 'year' and 'weekNumber' are required and must be greater than zero.",
                                StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        var driver =
                            await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);
                        }

                        var weekApproval = await db.WeekApprovals
                            .FirstOrDefaultAsync(wa =>
                                wa.DriverId == driver.Id &&
                                wa.Year == year &&
                                wa.WeekNr == weekNumber &&
                                wa.Status == WeekApprovalStatus.PendingDriver);

                        if (weekApproval == null)
                        {
                            return ApiResponseFactory.Error("Week not found or not eligible for signing.",
                                StatusCodes.Status404NotFound);
                        }

                        weekApproval.Status = WeekApprovalStatus.Signed;
                        weekApproval.DriverSignedAt = DateTime.UtcNow;

                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success(new
                        {
                            Message = "Week signed successfully.",
                            WeekApprovalId = weekApproval.Id,
                            Status = weekApproval.Status,
                            DriverSignedAt = weekApproval.DriverSignedAt
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}