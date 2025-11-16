using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs.Reports;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Services.Reports;
using TruckManagement.Enums;

namespace TruckManagement.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/reports/ride-executions",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customerAccountant, driver")]
            async (
                DateTime startDate,
                DateTime endDate,
                Guid? driverId,
                Guid? companyId,
                string? status,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser) =>
            {
                if (startDate > endDate)
                {
                    return ApiResponseFactory.Error("startDate must be before endDate.", StatusCodes.Status400BadRequest);
                }

                var userId = userManager.GetUserId(currentUser);
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);
                }

                var start = startDate.Date;
                var endExclusive = endDate.Date.AddDays(1);

                var isDriver = currentUser.IsInRole("driver");
                var isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                List<Guid>? allowedCompanyIds = null;

                if (isDriver)
                {
                    var currentDriver = await db.Drivers
                        .Include(d => d.Company)
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.AspNetUserId == userId);

                    if (currentDriver == null)
                    {
                        return ApiResponseFactory.Error("Driver record not found for current user.", StatusCodes.Status404NotFound);
                    }

                    if (driverId.HasValue && driverId.Value != currentDriver.Id)
                    {
                        return ApiResponseFactory.Error("Drivers can only access their own execution reports.", StatusCodes.Status403Forbidden);
                    }

                    driverId = currentDriver.Id;
                    allowedCompanyIds = currentDriver.CompanyId.HasValue
                        ? new List<Guid> { currentDriver.CompanyId.Value }
                        : new List<Guid>();
                }
                else if (!isGlobalAdmin)
                {
                    var contactPerson = await db.ContactPersons
                        .Include(cp => cp.ContactPersonClientCompanies)
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                    if (contactPerson == null)
                    {
                        return ApiResponseFactory.Error("User is not associated with any company.", StatusCodes.Status403Forbidden);
                    }

                    allowedCompanyIds = contactPerson.ContactPersonClientCompanies
                        .Where(cpc => cpc.CompanyId.HasValue)
                        .Select(cpc => cpc.CompanyId!.Value)
                        .Distinct()
                        .ToList();

                    if (allowedCompanyIds.Count == 0)
                    {
                        return ApiResponseFactory.Error("User does not have access to any companies.", StatusCodes.Status403Forbidden);
                    }
                }

                if (companyId.HasValue && !isGlobalAdmin)
                {
                    if (allowedCompanyIds == null || !allowedCompanyIds.Contains(companyId.Value))
                    {
                        return ApiResponseFactory.Error("You are not authorized to access this company.", StatusCodes.Status403Forbidden);
                    }
                }

                var executionsQuery = db.RideDriverExecutions
                    .Include(ex => ex.Driver)
                        .ThenInclude(d => d.User)
                    .Include(ex => ex.Driver)
                        .ThenInclude(d => d.Company)
                    .Include(ex => ex.Ride)
                        .ThenInclude(r => r.Company)
                    .Include(ex => ex.Ride)
                        .ThenInclude(r => r.Client)
                    .Include(ex => ex.HoursCode)
                    .Include(ex => ex.HoursOption)
                    .Where(ex => ex.Ride.PlannedDate.HasValue &&
                                 ex.Ride.PlannedDate.Value >= start &&
                                 ex.Ride.PlannedDate.Value < endExclusive);

                if (driverId.HasValue)
                {
                    executionsQuery = executionsQuery.Where(ex => ex.DriverId == driverId.Value);
                }

                if (companyId.HasValue)
                {
                    executionsQuery = executionsQuery.Where(ex => ex.Ride.CompanyId == companyId.Value);
                }
                else if (!isGlobalAdmin && allowedCompanyIds is not null)
                {
                    executionsQuery = executionsQuery.Where(ex => allowedCompanyIds.Contains(ex.Ride.CompanyId));
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse<RideDriverExecutionStatus>(status, true, out var parsedStatus))
                    {
                        executionsQuery = executionsQuery.Where(ex => ex.Status == parsedStatus);
                    }
                    else
                    {
                        return ApiResponseFactory.Error("Invalid status filter.", StatusCodes.Status400BadRequest);
                    }
                }

                var executions = await executionsQuery
                    .OrderBy(ex => ex.Ride.PlannedDate)
                    .ThenBy(ex => ex.Driver.User!.FirstName)
                    .ThenBy(ex => ex.Driver.User!.LastName)
                    .ToListAsync();

                var items = executions.Select(ex =>
                {
                    var actualKilometers = ex.ActualKilometers ?? 0;
                    var extraKilometers = ex.ExtraKilometers ?? 0;

                    return new RideExecutionReportItemDto
                    {
                        ExecutionId = ex.Id,
                        RideId = ex.RideId,
                        RideDate = ex.Ride.PlannedDate,
                        DriverId = ex.DriverId,
                        DriverFirstName = ex.Driver.User?.FirstName ?? string.Empty,
                        DriverLastName = ex.Driver.User?.LastName ?? string.Empty,
                        IsPrimary = ex.IsPrimary,
                        Status = ex.Status.ToString(),
                        HoursCodeName = ex.HoursCode?.Name,
                        HoursOptionName = ex.HoursOption?.Name,
                        DecimalHours = ex.DecimalHours ?? 0,
                        CorrectionTotalHours = ex.CorrectionTotalHours,
                        HourlyCompensation = ex.HourlyCompensation ?? 0,
                        NightAllowance = ex.NightAllowance ?? 0,
                        KilometerReimbursement = ex.KilometerReimbursement ?? 0,
                        ConsignmentFee = ex.ConsignmentFee ?? 0,
                        TaxFreeCompensation = ex.TaxFreeCompensation ?? 0,
                        VariousCompensation = ex.VariousCompensation ?? 0,
                        StandOver = ex.StandOver ?? 0,
                        SaturdayHours = ex.SaturdayHours ?? 0,
                        SundayHolidayHours = ex.SundayHolidayHours ?? 0,
                        VacationHoursEarned = ex.VacationHoursEarned ?? 0,
                        ExceedingContainerWaitingTime = ex.ExceedingContainerWaitingTime ?? 0,
                        ActualKilometers = actualKilometers,
                        ExtraKilometers = extraKilometers,
                        ActualCosts = ex.ActualCosts ?? 0,
                        Turnover = ex.Turnover ?? 0,
                        Remark = ex.Remark,
                        SubmittedAt = ex.SubmittedAt,
                        ApprovedAt = ex.ApprovedAt,
                        CompanyName = ex.Ride.Company?.Name ?? ex.Driver.Company?.Name ?? string.Empty,
                        ClientName = ex.Ride.Client?.Name
                    };
                }).ToList();

                var totals = new RideExecutionReportTotalsDto
                {
                    TotalExecutions = items.Count,
                    TotalHours = items.Sum(i => i.DecimalHours),
                    TotalCorrectedHours = items.Sum(i => i.CorrectionTotalHours),
                    TotalHourlyCompensation = items.Sum(i => i.HourlyCompensation),
                    TotalNightAllowance = items.Sum(i => i.NightAllowance),
                    TotalKilometerReimbursement = items.Sum(i => i.KilometerReimbursement),
                    TotalConsignmentFee = items.Sum(i => i.ConsignmentFee),
                    TotalTaxFreeCompensation = items.Sum(i => i.TaxFreeCompensation),
                    TotalVariousCompensation = items.Sum(i => i.VariousCompensation),
                    TotalStandOver = items.Sum(i => i.StandOver),
                    TotalSaturdayHours = items.Sum(i => i.SaturdayHours),
                    TotalSundayHolidayHours = items.Sum(i => i.SundayHolidayHours),
                    TotalVacationHoursEarned = items.Sum(i => i.VacationHoursEarned),
                    TotalExceedingContainerWaitingTime = items.Sum(i => i.ExceedingContainerWaitingTime),
                    TotalKilometers = items.Sum(i => i.ActualKilometers),
                    TotalExtraKilometers = items.Sum(i => i.ExtraKilometers),
                    TotalActualCosts = items.Sum(i => i.ActualCosts),
                    TotalTurnover = items.Sum(i => i.Turnover)
                };

                var driverSummaries = items
                    .GroupBy(i => i.DriverId)
                    .Select(group => new RideExecutionReportDriverSummaryDto
                    {
                        DriverId = group.Key,
                        DriverFirstName = group.Select(i => i.DriverFirstName).FirstOrDefault() ?? string.Empty,
                        DriverLastName = group.Select(i => i.DriverLastName).FirstOrDefault() ?? string.Empty,
                        CompanyId = executions
                            .Where(ex => ex.DriverId == group.Key)
                            .Select(ex => ex.Ride.CompanyId)
                            .FirstOrDefault(),
                        CompanyName = group.Select(i => i.CompanyName).FirstOrDefault() ?? string.Empty,
                        TotalHours = group.Sum(i => i.DecimalHours),
                        TotalCompensation = group.Sum(i => i.TotalCompensation),
                        TotalKilometers = group.Sum(i => i.ActualKilometers + i.ExtraKilometers),
                        ExecutionCount = group.Count()
                    })
                    .OrderBy(summary => summary.DriverLastName)
                    .ThenBy(summary => summary.DriverFirstName)
                    .ToList();

                var response = new RideExecutionReportResponseDto
                {
                    StartDate = start,
                    EndDate = endDate.Date,
                    DriverId = driverId,
                    CompanyId = companyId,
                    StatusFilter = status,
                    Items = items,
                    Totals = totals,
                    DriverSummaries = driverSummaries
                };

                return ApiResponseFactory.Success(response);
            })
            .WithName("GetRideExecutionReport")
            .WithSummary("Retrieve ride execution report data within a date range")
            .WithDescription("Returns per-driver ride execution data and aggregated totals for the specified period, optionally filtered by driver, company, and status.")
            .Produces(StatusCodes.Status200OK, typeof(RideExecutionReportResponseDto))
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Generate weekly PDF report for a driver
        app.MapGet("/reports/driver/{driverId}/week/{year}/{weekNumber}/pdf",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
            async (
                Guid driverId,
                int year,
                int weekNumber,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser) =>
            {
                try
                {
                    // 1. Validate parameters
                    if (year < 2020 || year > 2030)
                        return ApiResponseFactory.Error("Invalid year. Must be between 2020 and 2030.", StatusCodes.Status400BadRequest);
                    
                    if (weekNumber < 1 || weekNumber > 53)
                        return ApiResponseFactory.Error("Invalid week number. Must be between 1 and 53.", StatusCodes.Status400BadRequest);
                    
                    // 2. Authorization check
                    var authResult = await CheckDriverReportAuthorizationAsync(driverId, currentUser, userManager, db);
                    if (!authResult.IsAuthorized)
                        return ApiResponseFactory.Error(authResult.ErrorMessage, authResult.StatusCode);
                    
                    // 3. Create timeframe
                    var timeframe = ReportTimeframe.ForWeek(driverId, year, weekNumber);
                    
                    // 4. Generate report
                    var reportService = new ReportCalculationService(db);
                    var pdfGenerator = new DriverTimesheetPdfGenerator();
                    
                    var report = await reportService.BuildReportAsync(timeframe);
                    var pdfBytes = pdfGenerator.GenerateReportPdf(report);
                    
                    // 5. Return PDF file
                    var fileName = $"Driver_{driverId}_{year}_Week_{weekNumber}_Report.pdf";
                    return Results.File(pdfBytes, "application/pdf", fileName);
                }
                catch (ArgumentException ex)
                {
                    return ApiResponseFactory.Error(ex.Message, StatusCodes.Status404NotFound);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error generating weekly report: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                    return ApiResponseFactory.Error("Internal server error while generating report.", StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GenerateWeeklyDriverReport")
            .WithSummary("Generate PDF report for a driver's specific week")
            .WithDescription("Generates a detailed timesheet PDF report for a driver for a specific week, including hours breakdown, allowances, and vacation/TvT information.")
            .Produces(StatusCodes.Status200OK, typeof(byte[]), "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Generate period PDF report for a driver (4 weeks)
        app.MapGet("/reports/driver/{driverId}/period/{year}/{periodNumber}/pdf",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
            async (
                Guid driverId,
                int year,
                int periodNumber,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser) =>
            {
                try
                {
                    // 1. Validate parameters
                    if (year < 2020 || year > 2030)
                        return ApiResponseFactory.Error("Invalid year. Must be between 2020 and 2030.", StatusCodes.Status400BadRequest);
                    
                    if (periodNumber < 1 || periodNumber > 13)
                        return ApiResponseFactory.Error("Invalid period number. Must be between 1 and 13.", StatusCodes.Status400BadRequest);
                    
                    // 2. Authorization check
                    var authResult = await CheckDriverReportAuthorizationAsync(driverId, currentUser, userManager, db);
                    if (!authResult.IsAuthorized)
                        return ApiResponseFactory.Error(authResult.ErrorMessage, authResult.StatusCode);
                    
                    // 3. Create timeframe
                    var timeframe = ReportTimeframe.ForPeriod(driverId, year, periodNumber);
                    
                    // 4. Generate report
                    var reportService = new ReportCalculationService(db);
                    var pdfGenerator = new DriverTimesheetPdfGenerator();
                    
                    var report = await reportService.BuildReportAsync(timeframe);
                    var pdfBytes = pdfGenerator.GenerateReportPdf(report);
                    
                    // 5. Return PDF file
                    var fileName = $"Driver_{driverId}_{year}_Period_{periodNumber}_Report.pdf";
                    return Results.File(pdfBytes, "application/pdf", fileName);
                }
                catch (ArgumentException ex)
                {
                    return ApiResponseFactory.Error(ex.Message, StatusCodes.Status404NotFound);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error generating period report: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                    return ApiResponseFactory.Error("Internal server error while generating report.", StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("GeneratePeriodDriverReport")
            .WithSummary("Generate PDF report for a driver's specific 4-week period")
            .WithDescription("Generates a detailed timesheet PDF report for a driver for a specific 4-week period, including hours breakdown, allowances, and vacation/TvT information.")
            .Produces(StatusCodes.Status200OK, typeof(byte[]), "application/pdf")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
    
    private static async Task<AuthorizationResult> CheckDriverReportAuthorizationAsync(
        Guid driverId,
        ClaimsPrincipal currentUser,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        var userId = userManager.GetUserId(currentUser);
        if (string.IsNullOrEmpty(userId))
            return AuthorizationResult.Unauthorized("User not authenticated.");
        
        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
        bool isDriver = currentUser.IsInRole("driver");
        
        if (isGlobalAdmin)
        {
            // Global admins can access any driver's reports
            return AuthorizationResult.Authorized();
        }
        
        if (isDriver)
        {
            // Drivers can only access their own reports
            var currentDriver = await db.Drivers
                .FirstOrDefaultAsync(d => d.AspNetUserId == userId);
            
            if (currentDriver == null)
                return AuthorizationResult.Forbidden("Driver record not found for current user.");
            
            if (currentDriver.Id != driverId)
                return AuthorizationResult.Forbidden("Drivers can only access their own reports.");
            
            return AuthorizationResult.Authorized();
        }
        
        // For other roles (customerAdmin, employer, customer, customerAccountant)
        // Check if they have access to the driver's company
        var targetDriver = await db.Drivers
            .Include(d => d.Company)
            .FirstOrDefaultAsync(d => d.Id == driverId);
        
        if (targetDriver == null)
            return AuthorizationResult.NotFound("Driver not found.");
        
        var targetCompanyId = targetDriver.CompanyId;
        if (!targetCompanyId.HasValue)
            return AuthorizationResult.Forbidden("Driver is not associated with any company.");
        
        // Check user's company associations
        var contactPerson = await db.ContactPersons
            .Include(cp => cp.ContactPersonClientCompanies)
            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);
        
        if (contactPerson == null)
            return AuthorizationResult.Forbidden("User is not associated with any company.");
        
        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
            .Where(cpc => cpc.CompanyId.HasValue)
            .Select(cpc => cpc.CompanyId!.Value)
            .ToList();
        
        if (!associatedCompanyIds.Contains(targetCompanyId.Value))
            return AuthorizationResult.Forbidden("You are not authorized to access reports for this driver's company.");
        
        return AuthorizationResult.Authorized();
    }
}

public class AuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public string ErrorMessage { get; set; } = "";
    public int StatusCode { get; set; }
    
    public static AuthorizationResult Authorized()
    {
        return new AuthorizationResult { IsAuthorized = true };
    }
    
    public static AuthorizationResult Unauthorized(string message)
    {
        return new AuthorizationResult 
        { 
            IsAuthorized = false, 
            ErrorMessage = message, 
            StatusCode = StatusCodes.Status401Unauthorized 
        };
    }
    
    public static AuthorizationResult Forbidden(string message)
    {
        return new AuthorizationResult 
        { 
            IsAuthorized = false, 
            ErrorMessage = message, 
            StatusCode = StatusCodes.Status403Forbidden 
        };
    }
    
    public static AuthorizationResult NotFound(string message)
    {
        return new AuthorizationResult 
        { 
            IsAuthorized = false, 
            ErrorMessage = message, 
            StatusCode = StatusCodes.Status404NotFound 
        };
    }
} 