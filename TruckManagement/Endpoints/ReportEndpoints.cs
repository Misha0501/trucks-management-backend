using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs.Reports;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Services.Reports;

namespace TruckManagement.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
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