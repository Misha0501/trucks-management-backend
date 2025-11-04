using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Enums;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints;

public static class RideWeekSubmissionEndpoints
{
    public static WebApplication MapRideWeekSubmissionEndpoints(this WebApplication app)
    {
        // GET /rides/weeks-to-submit - Driver gets their own weeks or admin gets all weeks
        app.MapGet("/rides/weeks-to-submit",
            [Authorize(Roles = "driver, globalAdmin, customerAdmin, employer")]
            async (
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] Guid? driverId = null
            ) =>
            {
                try
                {
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var userRoles = await userManager.GetRolesAsync(user);
                    var isDriver = userRoles.Contains("driver");
                    var isAdmin = userRoles.Contains("globalAdmin") || userRoles.Contains("customerAdmin") || userRoles.Contains("employer");

                    Guid? targetDriverId = null;

                    if (isDriver)
                    {
                        // Drivers can only see their own weeks
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                        
                        if (driver == null)
                            return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                        targetDriverId = driver.Id;
                    }
                    else if (isAdmin)
                    {
                        // Admins can specify a driverId or see all
                        if (driverId.HasValue)
                        {
                            targetDriverId = driverId.Value;
                        }
                        // If no driverId specified, we'll return all drivers' weeks (handled below)
                    }

                    // Get all ride driver executions for the target driver(s)
                    var executionsQuery = db.RideDriverExecutions
                        .Include(e => e.Ride)
                        .Include(e => e.Driver)
                            .ThenInclude(d => d.User)
                        .Where(e => e.Status == RideDriverExecutionStatus.Approved); // Only approved executions

                    if (targetDriverId.HasValue)
                    {
                        executionsQuery = executionsQuery.Where(e => e.DriverId == targetDriverId.Value);
                    }

                    var executions = await executionsQuery.ToListAsync();

                    // Group by driver, year, and week number
                    var weekGroups = executions
                        .Where(e => e.WeekNumber.HasValue && e.Ride.PlannedDate.HasValue)
                        .GroupBy(e => new
                        {
                            e.DriverId,
                            Year = e.Ride.PlannedDate!.Value.Year,
                            WeekNumber = e.WeekNumber!.Value,
                            PeriodNumber = e.PeriodNumber ?? 0
                        })
                        .Select(g => new
                        {
                            DriverId = g.Key.DriverId,
                            Year = g.Key.Year,
                            WeekNumber = g.Key.WeekNumber,
                            PeriodNumber = g.Key.PeriodNumber,
                            Driver = new
                            {
                                Id = g.First().Driver.Id,
                                FirstName = g.First().Driver.User?.FirstName ?? "",
                                LastName = g.First().Driver.User?.LastName ?? ""
                            },
                            ExecutionCount = g.Count(),
                            TotalHours = Math.Round(g.Sum(e => e.DecimalHours ?? 0), 2),
                            TotalCompensation = Math.Round(g.Sum(e => 
                                (e.NightAllowance ?? 0) +
                                (e.KilometerReimbursement ?? 0) +
                                (e.ConsignmentFee ?? 0) +
                                (e.VariousCompensation ?? 0) +
                                (e.TaxFreeCompensation ?? 0)
                            ), 2),
                            // Check if this week has already been submitted
                            WeekStartDate = GetWeekStartDate(g.Key.Year, g.Key.WeekNumber)
                        })
                        .OrderByDescending(w => w.Year)
                        .ThenByDescending(w => w.WeekNumber)
                        .ToList();

                    // Now check which weeks have been submitted and create/get WeekApproval records
                    var weeksWithSubmission = new List<object>();
                    foreach (var week in weekGroups)
                    {
                        var weekApproval = await db.WeekApprovals
                            .FirstOrDefaultAsync(wa => 
                                wa.DriverId == week.DriverId &&
                                wa.Year == week.Year &&
                                wa.WeekNr == week.WeekNumber);

                        // Auto-create WeekApproval record if it doesn't exist (for admin workflow)
                        if (weekApproval == null && isAdmin)
                        {
                            weekApproval = new WeekApproval
                            {
                                Id = Guid.NewGuid(),
                                DriverId = week.DriverId,
                                Year = week.Year,
                                WeekNr = week.WeekNumber,
                                PeriodNr = week.PeriodNumber,
                                Status = WeekApprovalStatus.PendingAdmin, // Start as PendingAdmin for admin workflow
                                AdminUserId = null,
                                AdminAllowedAt = null,
                                DriverSignedAt = null
                            };
                            db.WeekApprovals.Add(weekApproval);
                        }

                        // Determine summary status based on executions
                        var allExecutions = await db.RideDriverExecutions
                            .Include(e => e.Ride)
                            .Where(e => e.DriverId == week.DriverId &&
                                       e.Ride.PlannedDate.HasValue &&
                                       e.Ride.PlannedDate.Value.Year == week.Year &&
                                       e.WeekNumber == week.WeekNumber)
                            .ToListAsync();

                        var hasDisputes = allExecutions.Any(e => e.Status == RideDriverExecutionStatus.Dispute);
                        var hasRejected = allExecutions.Any(e => e.Status == RideDriverExecutionStatus.Rejected);
                        var hasPending = allExecutions.Any(e => e.Status == RideDriverExecutionStatus.Pending);
                        var allApproved = allExecutions.All(e => e.Status == RideDriverExecutionStatus.Approved);

                        var summaryStatus = hasDisputes ? "Has Disputes" :
                                          hasRejected ? "Has Rejected" :
                                          hasPending ? "Has Pending" :
                                          allApproved ? "All Approved" :
                                          "Unknown";

                        weeksWithSubmission.Add(new
                        {
                            Id = weekApproval?.Id, // CRITICAL: Include WeekApproval ID for admin actions
                            week.DriverId,
                            week.Year,
                            week.WeekNumber,
                            week.PeriodNumber,
                            week.Driver,
                            week.ExecutionCount,
                            week.TotalHours,
                            week.TotalCompensation,
                            week.WeekStartDate,
                            IsSubmitted = weekApproval != null && weekApproval.Status != WeekApprovalStatus.PendingAdmin,
                            Status = weekApproval?.Status.ToString() ?? "PendingAdmin",
                            SummaryStatus = summaryStatus, // Add summary status like partride system
                            SubmittedAt = weekApproval?.AdminAllowedAt,
                            SignedAt = weekApproval?.DriverSignedAt,
                            PendingCount = allExecutions.Count(e => e.Status == RideDriverExecutionStatus.Pending),
                            DisputeCount = allExecutions.Count(e => e.Status == RideDriverExecutionStatus.Dispute),
                            RejectedCount = allExecutions.Count(e => e.Status == RideDriverExecutionStatus.Rejected)
                        });
                    }

                    // Save any newly created week approvals
                    if (isAdmin && db.ChangeTracker.HasChanges())
                    {
                        await db.SaveChangesAsync();
                    }

                    return ApiResponseFactory.Success(weeksWithSubmission);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error retrieving weeks: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // GET /rides/week/{weekStartDate} - Get detailed week data for a driver
        app.MapGet("/rides/week/{weekStartDate}",
            [Authorize(Roles = "driver, globalAdmin, customerAdmin, employer")]
            async (
                string weekStartDate,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] Guid? driverId = null
            ) =>
            {
                try
                {
                    if (!DateOnly.TryParseExact(weekStartDate, "yyyy-MM-dd", out var startDate))
                        return ApiResponseFactory.Error("Invalid date format. Use yyyy-MM-dd.", StatusCodes.Status400BadRequest);

                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var userRoles = await userManager.GetRolesAsync(user);
                    var isDriver = userRoles.Contains("driver");

                    Guid targetDriverId;

                    if (isDriver)
                    {
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                        
                        if (driver == null)
                            return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                        targetDriverId = driver.Id;
                    }
                    else
                    {
                        if (!driverId.HasValue)
                            return ApiResponseFactory.Error("driverId parameter required for admin users.", StatusCodes.Status400BadRequest);

                        targetDriverId = driverId.Value;
                    }

                    // Calculate week number from date
                    var (year, weekNumber) = GetIsoWeekNumber(startDate);

                    // Get all executions for this driver in this week
                    var executions = await db.RideDriverExecutions
                        .Include(e => e.Ride)
                            .ThenInclude(r => r.Client)
                        .Include(e => e.HoursCode)
                        .Include(e => e.Driver)
                            .ThenInclude(d => d.User)
                        .Where(e => e.DriverId == targetDriverId &&
                                   e.Ride.PlannedDate.HasValue &&
                                   e.Ride.PlannedDate.Value.Year == year &&
                                   e.WeekNumber == weekNumber &&
                                   e.Status == RideDriverExecutionStatus.Approved)
                        .OrderBy(e => e.Ride.PlannedDate)
                        .ToListAsync();

                    if (executions.Count == 0)
                        return ApiResponseFactory.Error("No approved executions found for this week.", StatusCodes.Status404NotFound);

                    var executionDetails = executions.Select(e => new
                    {
                        ExecutionId = e.Id,
                        RideId = e.RideId,
                        Date = e.Ride.PlannedDate?.ToString("yyyy-MM-dd"),
                        ClientName = e.Ride.Client?.Name ?? "Unknown",
                        Hours = Math.Round(e.DecimalHours ?? 0, 2),
                        HoursCode = e.HoursCode != null ? new { e.HoursCode.Id, e.HoursCode.Name } : null,
                        Compensation = Math.Round(
                            (e.NightAllowance ?? 0) +
                            (e.KilometerReimbursement ?? 0) +
                            (e.ConsignmentFee ?? 0) +
                            (e.VariousCompensation ?? 0) +
                            (e.TaxFreeCompensation ?? 0), 2)
                    }).ToList();

                    var totalHours = Math.Round(executions.Sum(e => e.DecimalHours ?? 0), 2);
                    var totalCompensation = Math.Round(executions.Sum(e =>
                        (e.NightAllowance ?? 0) +
                        (e.KilometerReimbursement ?? 0) +
                        (e.ConsignmentFee ?? 0) +
                        (e.VariousCompensation ?? 0) +
                        (e.TaxFreeCompensation ?? 0)), 2);

                    // Check if week is already submitted
                    var weekApproval = await db.WeekApprovals
                        .FirstOrDefaultAsync(wa =>
                            wa.DriverId == targetDriverId &&
                            wa.Year == year &&
                            wa.WeekNr == weekNumber);

                    return ApiResponseFactory.Success(new
                    {
                        Id = weekApproval?.Id, // Include ID for admin actions
                        DriverId = targetDriverId,
                        Driver = new
                        {
                            Id = executions.First().Driver.Id,
                            FirstName = executions.First().Driver.User?.FirstName ?? "",
                            LastName = executions.First().Driver.User?.LastName ?? ""
                        },
                        Year = year,
                        WeekNumber = weekNumber,
                        PeriodNumber = executions.First().PeriodNumber ?? 0,
                        WeekStartDate = startDate.ToString("yyyy-MM-dd"),
                        TotalHours = totalHours,
                        TotalCompensation = totalCompensation,
                        ExecutionCount = executions.Count,
                        Executions = executionDetails,
                        IsSubmitted = weekApproval != null && weekApproval.Status != WeekApprovalStatus.PendingAdmin,
                        Status = weekApproval?.Status.ToString() ?? "PendingAdmin",
                        SubmittedAt = weekApproval?.AdminAllowedAt,
                        SignedAt = weekApproval?.DriverSignedAt
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error retrieving week details: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // PUT /rides/week/{weekStartDate}/submit - Driver submits their week
        app.MapPut("/rides/week/{weekStartDate}/submit",
            [Authorize(Roles = "driver")]
            async (
                string weekStartDate,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (!DateOnly.TryParseExact(weekStartDate, "yyyy-MM-dd", out var startDate))
                        return ApiResponseFactory.Error("Invalid date format. Use yyyy-MM-dd.", StatusCodes.Status400BadRequest);

                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    var (year, weekNumber) = GetIsoWeekNumber(startDate);

                    // Get all executions for this driver in this week
                    var executions = await db.RideDriverExecutions
                        .Include(e => e.Ride)
                        .Where(e => e.DriverId == driver.Id &&
                                   e.Ride.PlannedDate.HasValue &&
                                   e.Ride.PlannedDate.Value.Year == year &&
                                   e.WeekNumber == weekNumber &&
                                   e.Status == RideDriverExecutionStatus.Approved)
                        .ToListAsync();

                    if (executions.Count == 0)
                        return ApiResponseFactory.Error("No approved executions found for this week. Cannot submit.", StatusCodes.Status400BadRequest);

                    // Check if already submitted
                    var existingApproval = await db.WeekApprovals
                        .FirstOrDefaultAsync(wa =>
                            wa.DriverId == driver.Id &&
                            wa.Year == year &&
                            wa.WeekNr == weekNumber);

                    if (existingApproval != null)
                        return ApiResponseFactory.Error("This week has already been submitted.", StatusCodes.Status409Conflict);

                    // Calculate period number from week number (4-week periods)
                    var periodNumber = ((weekNumber - 1) / 4) + 1;

                    // Create week approval
                    var weekApproval = new WeekApproval
                    {
                        Id = Guid.NewGuid(),
                        DriverId = driver.Id,
                        Year = year,
                        WeekNr = weekNumber,
                        PeriodNr = periodNumber,
                        Status = WeekApprovalStatus.PendingAdmin,
                        AdminUserId = null,
                        AdminAllowedAt = null,
                        DriverSignedAt = null
                    };

                    db.WeekApprovals.Add(weekApproval);
                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(new
                    {
                        Id = weekApproval.Id,
                        DriverId = driver.Id,
                        Year = year,
                        WeekNumber = weekNumber,
                        PeriodNumber = periodNumber,
                        Status = weekApproval.Status.ToString(),
                        Message = "Week submitted successfully for admin approval."
                    }, StatusCodes.Status201Created);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error submitting week: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // GET /rides/periods/driver/pending - Driver gets their pending periods
        app.MapGet("/rides/periods/driver/pending",
            [Authorize(Roles = "driver")]
            async (
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10
            ) =>
            {
                try
                {
                    if (pageNumber < 1) pageNumber = 1;
                    if (pageSize < 1) pageSize = 10;

                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Get all week approvals for this driver
                    var weekApprovals = await db.WeekApprovals
                        .Where(wa => wa.DriverId == driver.Id)
                        .ToListAsync();

                    // Group by period
                    var periodGroups = weekApprovals
                        .GroupBy(wa => new { wa.Year, wa.PeriodNr })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            PeriodNr = g.Key.PeriodNr,
                            Weeks = g.OrderBy(w => w.WeekNr).ToList(),
                            WeekCount = g.Count(),
                            SignedWeekCount = g.Count(w => w.Status == WeekApprovalStatus.Signed),
                            PendingWeekCount = g.Count(w => w.Status == WeekApprovalStatus.PendingDriver),
                            HasPendingWeeks = g.Any(w => w.Status == WeekApprovalStatus.PendingDriver)
                        })
                        .Where(p => p.HasPendingWeeks) // Only periods with pending weeks
                        .OrderByDescending(p => p.Year)
                        .ThenByDescending(p => p.PeriodNr)
                        .ToList();

                    // Pagination
                    var totalCount = periodGroups.Count;
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    var pagedPeriods = periodGroups
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(p => new
                        {
                            Year = p.Year,
                            PeriodNr = p.PeriodNr,
                            Status = 1, // PendingDriver (since we filtered for pending weeks)
                            FromDate = GetWeekStartDate(p.Year, p.Weeks.First().WeekNr),
                            ToDate = GetWeekEndDate(p.Year, p.Weeks.Last().WeekNr),
                            WeekCount = p.WeekCount,
                            SignedWeekCount = p.SignedWeekCount,
                            PendingWeekCount = p.PendingWeekCount
                        })
                        .ToList();

                    return ApiResponseFactory.Success(new
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        Data = pagedPeriods
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error retrieving pending periods: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // GET /rides/drivers/week/details - Driver gets their week details for signing
        app.MapGet("/rides/drivers/week/details",
            [Authorize(Roles = "driver")]
            async (
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] int year,
                [FromQuery] int weekNumber
            ) =>
            {
                try
                {
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Find the week approval
                    var weekApproval = await db.WeekApprovals
                        .FirstOrDefaultAsync(wa =>
                            wa.DriverId == driver.Id &&
                            wa.Year == year &&
                            wa.WeekNr == weekNumber);

                    if (weekApproval == null)
                        return ApiResponseFactory.Error("Week approval not found.", StatusCodes.Status404NotFound);

                    // Get all executions for this week
                    var executions = await db.RideDriverExecutions
                        .Include(e => e.Ride)
                            .ThenInclude(r => r.Client)
                        .Include(e => e.HoursCode)
                        .Where(e => e.DriverId == driver.Id &&
                                   e.Ride.PlannedDate.HasValue &&
                                   e.Ride.PlannedDate.Value.Year == year &&
                                   e.WeekNumber == weekNumber &&
                                   e.Status == RideDriverExecutionStatus.Approved)
                        .OrderBy(e => e.Ride.PlannedDate)
                        .ToListAsync();

                    var executionDetails = executions.Select(e => new
                    {
                        RideId = e.RideId,
                        Date = e.Ride.PlannedDate?.ToString("yyyy-MM-dd"),
                        ClientName = e.Ride.Client?.Name ?? "Unknown",
                        ActualStartTime = e.ActualStartTime?.ToString(@"hh\:mm"),
                        ActualEndTime = e.ActualEndTime?.ToString(@"hh\:mm"),
                        ActualRestTime = e.ActualRestTime?.ToString(@"hh\:mm"),
                        TotalHours = Math.Round(e.DecimalHours ?? 0, 2),
                        Compensation = Math.Round(
                            (e.NightAllowance ?? 0) +
                            (e.KilometerReimbursement ?? 0) +
                            (e.ConsignmentFee ?? 0) +
                            (e.VariousCompensation ?? 0) +
                            (e.TaxFreeCompensation ?? 0), 2),
                        HoursCodeName = e.HoursCode?.Name
                    }).ToList();

                    var totalHours = Math.Round(executions.Sum(e => e.DecimalHours ?? 0), 2);
                    var totalCompensation = Math.Round(executions.Sum(e =>
                        (e.NightAllowance ?? 0) +
                        (e.KilometerReimbursement ?? 0) +
                        (e.ConsignmentFee ?? 0) +
                        (e.VariousCompensation ?? 0) +
                        (e.TaxFreeCompensation ?? 0)), 2);

                    return ApiResponseFactory.Success(new
                    {
                        WeekApprovalId = weekApproval.Id,
                        Year = weekApproval.Year,
                        WeekNumber = weekApproval.WeekNr,
                        Status = (int)weekApproval.Status,
                        TotalHours = totalHours,
                        TotalCompensation = totalCompensation,
                        AdminAllowedAt = weekApproval.AdminAllowedAt,
                        Executions = executionDetails
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error retrieving week details: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // PUT /rides/weeks-to-submit/{id}/sign - Driver signs their week
        app.MapPut("/rides/weeks-to-submit/{id}/sign",
            [Authorize(Roles = "driver")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (!Guid.TryParse(id, out var weekApprovalId))
                        return ApiResponseFactory.Error("Invalid week approval ID format.", StatusCodes.Status400BadRequest);

                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Find the week approval
                    var weekApproval = await db.WeekApprovals
                        .FirstOrDefaultAsync(wa => wa.Id == weekApprovalId);

                    if (weekApproval == null)
                        return ApiResponseFactory.Error("Week approval not found.", StatusCodes.Status404NotFound);

                    // Verify this is the driver's week
                    if (weekApproval.DriverId != driver.Id)
                        return ApiResponseFactory.Error("You are not authorized to sign this week.", StatusCodes.Status403Forbidden);

                    // Check if week is in correct status
                    if (weekApproval.Status != WeekApprovalStatus.PendingDriver)
                        return ApiResponseFactory.Error("Week is not in PendingDriver status.", StatusCodes.Status409Conflict);

                    // Update week status to signed
                    weekApproval.Status = WeekApprovalStatus.Signed;
                    weekApproval.DriverSignedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(new
                    {
                        Id = weekApproval.Id,
                        DriverId = weekApproval.DriverId,
                        Year = weekApproval.Year,
                        WeekNumber = weekApproval.WeekNr,
                        NewStatus = weekApproval.Status.ToString(),
                        SignedAt = weekApproval.DriverSignedAt,
                        Message = "Week signed successfully."
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error signing week: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // PUT /rides/weeks-to-submit/{id}/allow-driver - Admin allows driver to sign their week
        app.MapPut("/rides/weeks-to-submit/{id}/allow-driver",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (!Guid.TryParse(id, out var weekApprovalId))
                        return ApiResponseFactory.Error("Invalid week approval ID format.", StatusCodes.Status400BadRequest);

                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var userRoles = await userManager.GetRolesAsync(user);
                    var isGlobalAdmin = userRoles.Contains("globalAdmin");

                    // Find the week approval
                    var weekApproval = await db.WeekApprovals
                        .FirstOrDefaultAsync(wa => wa.Id == weekApprovalId);

                    if (weekApproval == null)
                        return ApiResponseFactory.Error("Week approval not found.", StatusCodes.Status404NotFound);

                    // Check if week is in correct status
                    if (weekApproval.Status != WeekApprovalStatus.PendingAdmin)
                        return ApiResponseFactory.Error("Week is not in PendingAdmin status.", StatusCodes.Status409Conflict);

                    // Get all executions for this week to validate
                    var executions = await db.RideDriverExecutions
                        .Include(e => e.Ride)
                        .Where(e => e.DriverId == weekApproval.DriverId &&
                                   e.Ride.PlannedDate.HasValue &&
                                   e.Ride.PlannedDate.Value.Year == weekApproval.Year &&
                                   e.WeekNumber == weekApproval.WeekNr)
                        .ToListAsync();

                    // Check for any disputes
                    if (executions.Any(e => e.Status == RideDriverExecutionStatus.Dispute))
                        return ApiResponseFactory.Error("Week has executions in Dispute status.", StatusCodes.Status409Conflict);

                    // Check if all are approved
                    var allApproved = executions.All(e => e.Status == RideDriverExecutionStatus.Approved);
                    if (!allApproved)
                        return ApiResponseFactory.Error("All executions must be Approved before allowing driver signature.", StatusCodes.Status409Conflict);

                    // Update week status to allow driver to sign
                    weekApproval.Status = WeekApprovalStatus.PendingDriver;
                    weekApproval.AdminUserId = Guid.TryParse(userId, out var adminGuid) ? adminGuid : null;
                    weekApproval.AdminAllowedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(new
                    {
                        Id = weekApproval.Id,
                        DriverId = weekApproval.DriverId,
                        Year = weekApproval.Year,
                        WeekNumber = weekApproval.WeekNr,
                        NewStatus = weekApproval.Status.ToString(),
                        Message = "Week submitted to driver for signature."
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error allowing driver to sign: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        return app;
    }

    private static (int year, int weekNumber) GetIsoWeekNumber(DateOnly date)
    {
        var calendar = CultureInfo.CurrentCulture.Calendar;
        var dayOfWeek = calendar.GetDayOfWeek(date.ToDateTime(TimeOnly.MinValue));
        
        // ISO 8601 week calculation
        if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }

        var weekNumber = calendar.GetWeekOfYear(
            date.ToDateTime(TimeOnly.MinValue),
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);

        var year = date.Year;
        if (weekNumber >= 52 && date.Month == 1)
        {
            year--;
        }
        else if (weekNumber == 1 && date.Month == 12)
        {
            year++;
        }

        return (year, weekNumber);
    }

    private static string GetWeekStartDate(int year, int weekNumber)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        if (daysOffset < 0) daysOffset += 7;
        
        var firstMonday = jan1.AddDays(daysOffset);
        var weekStart = firstMonday.AddDays((weekNumber - 1) * 7);
        
        return weekStart.ToString("yyyy-MM-dd");
    }

    private static string GetWeekEndDate(int year, int weekNumber)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        if (daysOffset < 0) daysOffset += 7;
        
        var firstMonday = jan1.AddDays(daysOffset);
        var weekEnd = firstMonday.AddDays((weekNumber - 1) * 7 + 6); // Add 6 days to get Sunday
        
        return weekEnd.ToString("yyyy-MM-dd");
    }
}

