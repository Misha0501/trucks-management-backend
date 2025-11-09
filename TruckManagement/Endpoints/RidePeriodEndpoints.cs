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
using TruckManagement.Services.Reports;

namespace TruckManagement.Endpoints;

public static class RidePeriodEndpoints
{
    public static WebApplication MapRidePeriodEndpoints(this WebApplication app)
    {
        // GET /rides/periods/{year}/{periodNumber} - Get driver's period details from ride executions
        app.MapGet("/rides/periods/{year}/{periodNumber}",
            [Authorize(Roles = "driver")]
            async (
                int year,
                int periodNumber,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    // Validate parameters
                    if (year < 2020 || year > 2030)
                        return ApiResponseFactory.Error("Invalid year. Must be between 2020 and 2030.", StatusCodes.Status400BadRequest);
                    
                    if (periodNumber < 1 || periodNumber > 13)
                        return ApiResponseFactory.Error("Invalid period number. Must be between 1 and 13.", StatusCodes.Status400BadRequest);

                    // Get current driver
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var driver = await db.Drivers
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Calculate period date range (periods are 4 weeks each)
                    var firstWeekOfPeriod = ((periodNumber - 1) * 4) + 1;
                    var lastWeekOfPeriod = firstWeekOfPeriod + 3;

                    var fromDate = GetWeekStartDate(year, firstWeekOfPeriod);
                    var toDate = GetWeekEndDate(year, lastWeekOfPeriod);

                    // Get all week approvals for this period
                    var weekApprovals = await db.WeekApprovals
                        .Where(wa => 
                            wa.DriverId == driver.Id &&
                            wa.Year == year &&
                            wa.PeriodNr == periodNumber)
                        .OrderBy(wa => wa.WeekNr)
                        .ToListAsync();

                    // Get all executions for this period
                    var executions = await db.RideDriverExecutions
                        .Include(e => e.Ride)
                        .Where(e => 
                            e.DriverId == driver.Id &&
                            e.Ride.PlannedDate.HasValue &&
                            e.Ride.PlannedDate.Value.Year == year &&
                            e.PeriodNumber == periodNumber &&
                            e.Status == RideDriverExecutionStatus.Approved)
                        .ToListAsync();

                    // Calculate totals
                    var totalHours = Math.Round(executions.Sum(e => e.DecimalHours ?? 0), 2);
                    var totalCompensation = Math.Round(executions.Sum(e =>
                        (e.NightAllowance ?? 0) +
                        (e.KilometerReimbursement ?? 0) +
                        (e.ConsignmentFee ?? 0) +
                        (e.VariousCompensation ?? 0) +
                        (e.TaxFreeCompensation ?? 0)), 2);

                    // Build week summaries
                    var weekSummaries = new List<object>();
                    for (int i = 0; i < 4; i++)
                    {
                        var weekNr = firstWeekOfPeriod + i;
                        var weekApproval = weekApprovals.FirstOrDefault(wa => wa.WeekNr == weekNr);
                        var weekExecutions = executions.Where(e => e.WeekNumber == weekNr).ToList();

                        weekSummaries.Add(new
                        {
                            WeekNumber = weekNr,
                            WeekInPeriod = i + 1,
                            TotalHours = Math.Round(weekExecutions.Sum(e => e.DecimalHours ?? 0), 2),
                            TotalCompensation = Math.Round(weekExecutions.Sum(e =>
                                (e.NightAllowance ?? 0) +
                                (e.KilometerReimbursement ?? 0) +
                                (e.ConsignmentFee ?? 0) +
                                (e.VariousCompensation ?? 0) +
                                (e.TaxFreeCompensation ?? 0)), 2),
                            ExecutionCount = weekExecutions.Count,
                            Status = weekApproval?.Status ?? WeekApprovalStatus.PendingAdmin
                        });
                    }

                    // Determine period status
                    var allWeeksSigned = weekApprovals.Count == 4 && weekApprovals.All(wa => wa.Status == WeekApprovalStatus.Signed);
                    var periodApproval = await db.RidePeriodApprovals
                        .FirstOrDefaultAsync(rpa =>
                            rpa.DriverId == driver.Id &&
                            rpa.Year == year &&
                            rpa.PeriodNr == periodNumber);

                    var status = periodApproval?.Status ?? (allWeeksSigned ? RidePeriodApprovalStatus.ReadyToSign : RidePeriodApprovalStatus.NotReady);

                    return ApiResponseFactory.Success(new
                    {
                        Year = year,
                        PeriodNumber = periodNumber,
                        DriverId = driver.Id,
                        Driver = new
                        {
                            FirstName = driver.User.FirstName,
                            LastName = driver.User.LastName
                        },
                        Status = (int)status,
                        FromDate = fromDate,
                        ToDate = toDate,
                        TotalHours = totalHours,
                        TotalCompensation = totalCompensation,
                        Weeks = weekSummaries,
                        SignedAt = periodApproval?.DriverSignedAt
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error retrieving period details: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // PUT /rides/period/{year}/{periodNumber}/sign-driver - Driver signs their period
        app.MapPut("/rides/period/{year}/{periodNumber}/sign-driver",
            [Authorize(Roles = "driver")]
            async (
                int year,
                int periodNumber,
                [FromBody] SignPeriodRequest request,
                HttpContext httpContext,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    // Validate parameters
                    if (year < 2020 || year > 2030)
                        return ApiResponseFactory.Error("Invalid year. Must be between 2020 and 2030.", StatusCodes.Status400BadRequest);
                    
                    if (periodNumber < 1 || periodNumber > 13)
                        return ApiResponseFactory.Error("Invalid period number. Must be between 1 and 13.", StatusCodes.Status400BadRequest);

                    if (string.IsNullOrWhiteSpace(request.Signature))
                        return ApiResponseFactory.Error("Signature is required.", StatusCodes.Status400BadRequest);

                    // Get current driver
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var driver = await db.Drivers
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Check if all 4 weeks are signed
                    var weekApprovals = await db.WeekApprovals
                        .Where(wa => 
                            wa.DriverId == driver.Id &&
                            wa.Year == year &&
                            wa.PeriodNr == periodNumber)
                        .ToListAsync();

                    if (weekApprovals.Count != 4 || !weekApprovals.All(wa => wa.Status == WeekApprovalStatus.Signed))
                        return ApiResponseFactory.Error("All 4 weeks must be signed before signing the period.", StatusCodes.Status409Conflict);

                    // Calculate period dates
                    var firstWeekOfPeriod = ((periodNumber - 1) * 4) + 1;
                    var lastWeekOfPeriod = firstWeekOfPeriod + 3;
                    var fromDate = DateTime.ParseExact(GetWeekStartDate(year, firstWeekOfPeriod), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var toDate = DateTime.ParseExact(GetWeekEndDate(year, lastWeekOfPeriod), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    // Get executions for total calculations
                    var executions = await db.RideDriverExecutions
                        .Include(e => e.Ride)
                        .Where(e => 
                            e.DriverId == driver.Id &&
                            e.Ride.PlannedDate.HasValue &&
                            e.Ride.PlannedDate.Value.Year == year &&
                            e.PeriodNumber == periodNumber &&
                            e.Status == RideDriverExecutionStatus.Approved)
                        .ToListAsync();

                    var totalHours = (decimal)Math.Round(executions.Sum(e => e.DecimalHours ?? 0), 2);
                    var totalCompensation = (decimal)Math.Round(executions.Sum(e =>
                        (e.NightAllowance ?? 0) +
                        (e.KilometerReimbursement ?? 0) +
                        (e.ConsignmentFee ?? 0) +
                        (e.VariousCompensation ?? 0) +
                        (e.TaxFreeCompensation ?? 0)), 2);

                    // Create or update period approval
                    var periodApproval = await db.RidePeriodApprovals
                        .FirstOrDefaultAsync(rpa =>
                            rpa.DriverId == driver.Id &&
                            rpa.Year == year &&
                            rpa.PeriodNr == periodNumber);

                    if (periodApproval == null)
                    {
                        periodApproval = new RidePeriodApproval
                        {
                            Id = Guid.NewGuid(),
                            DriverId = driver.Id,
                            Year = year,
                            PeriodNr = periodNumber,
                            FromDate = fromDate,
                            ToDate = toDate
                        };
                        db.RidePeriodApprovals.Add(periodApproval);
                    }

                    // Update signature information
                    periodApproval.DriverSignedAt = DateTime.UtcNow;
                    periodApproval.DriverSignatureData = request.Signature;
                    periodApproval.DriverSignedIp = httpContext.Connection.RemoteIpAddress?.ToString();
                    periodApproval.DriverSignedUserAgent = httpContext.Request.Headers["User-Agent"].ToString();
                    periodApproval.Status = RidePeriodApprovalStatus.Signed;
                    periodApproval.TotalHours = totalHours;
                    periodApproval.TotalCompensation = totalCompensation;

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(new
                    {
                        Success = true,
                        SignedAt = periodApproval.DriverSignedAt,
                        PeriodId = periodApproval.Id,
                        Message = "Period signed successfully."
                    });
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error signing period: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        // GET /rides/period/{year}/{periodNumber}/pdf - Generate period PDF from ride executions
        app.MapGet("/rides/period/{year}/{periodNumber}/pdf",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, driver")]
            async (
                int year,
                int periodNumber,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    // Validate parameters
                    if (year < 2020 || year > 2030)
                        return ApiResponseFactory.Error("Invalid year. Must be between 2020 and 2030.", StatusCodes.Status400BadRequest);
                    
                    if (periodNumber < 1 || periodNumber > 13)
                        return ApiResponseFactory.Error("Invalid period number. Must be between 1 and 13.", StatusCodes.Status400BadRequest);

                    // Get current driver
                    var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var user = await userManager.FindByIdAsync(userId!);
                    if (user == null)
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);

                    var driver = await db.Drivers
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                    
                    if (driver == null)
                        return ApiResponseFactory.Error("Driver profile not found.", StatusCodes.Status403Forbidden);

                    // Check if period is signed
                    var periodApproval = await db.RidePeriodApprovals
                        .FirstOrDefaultAsync(rpa =>
                            rpa.DriverId == driver.Id &&
                            rpa.Year == year &&
                            rpa.PeriodNr == periodNumber);

                    if (periodApproval == null || periodApproval.Status != RidePeriodApprovalStatus.Signed)
                        return ApiResponseFactory.Error("Period must be signed before generating PDF.", StatusCodes.Status409Conflict);

                    // Create timeframe for report generation (reusing existing infrastructure)
                    var timeframe = DTOs.Reports.ReportTimeframe.ForPeriod(driver.Id, year, periodNumber);

                    // Generate report using existing service (but with execution data instead of partride)
                    var reportService = new ReportCalculationService(db);
                    var pdfGenerator = new DriverTimesheetPdfGenerator();

                    var report = await reportService.BuildReportAsync(timeframe);
                    var pdfBytes = pdfGenerator.GenerateReportPdf(report);

                    // Return PDF file
                    var fileName = $"Driver_{driver.Id}_{year}_Period_{periodNumber}_Report.pdf";
                    return Results.File(pdfBytes, "application/pdf", fileName);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error generating period PDF: {ex.Message}", StatusCodes.Status500InternalServerError);
                }
            });

        return app;
    }

    // Helper methods
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
        var weekEnd = firstMonday.AddDays((weekNumber - 1) * 7 + 6);
        
        return weekEnd.ToString("yyyy-MM-dd");
    }
}

// DTO for signing request
public class SignPeriodRequest
{
    public string Signature { get; set; } = default!;  // Base64 signature image
}

