using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Helpers;
using TruckManagement.Services;

namespace TruckManagement.Endpoints
{
    public static class DriverInvoiceEndpoints
    {
        public static void MapDriverInvoiceEndpoints(this IEndpointRouteBuilder app)
        {
            // POST /drivers/{driverId}/weeks/{weekNumber}/invoice - Generate weekly invoice
            app.MapPost("/drivers/{driverId}/weeks/{weekNumber}/invoice",
                [Authorize(Roles = "driver")]
                async (
                    Guid driverId,
                    int weekNumber,
                    [FromBody] GenerateDriverInvoiceRequest request,
                    ApplicationDbContext db,
                    DriverInvoiceService invoiceService,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Authorization: verify driver can only generate their own invoice
                        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
                        
                        if (driver == null)
                        {
                            return ApiResponseFactory.Error(
                                "Driver not found.",
                                StatusCodes.Status404NotFound);
                        }
                        
                        if (driver.AspNetUserId != userId)
                        {
                            return ApiResponseFactory.Error(
                                "You can only generate invoices for yourself.",
                                StatusCodes.Status403Forbidden);
                        }
                        
                        // Validate request
                        if (request.Year < 2000 || request.Year > 2100)
                        {
                            return ApiResponseFactory.Error(
                                "Invalid year. Must be between 2000 and 2100.",
                                StatusCodes.Status400BadRequest);
                        }
                        
                        if (request.WeekNumber < 1 || request.WeekNumber > 53)
                        {
                            return ApiResponseFactory.Error(
                                "Invalid week number. Must be between 1 and 53.",
                                StatusCodes.Status400BadRequest);
                        }
                        
                        if (request.HoursWorked < 0 || request.HourlyCompensation < 0 || request.AdditionalCompensation < 0)
                        {
                            return ApiResponseFactory.Error(
                                "Hours and compensation values cannot be negative.",
                                StatusCodes.Status400BadRequest);
                        }
                        
                        // Generate invoice
                        var pdfBytes = await invoiceService.GenerateWeekInvoiceAsync(
                            driverId,
                            request.Year,
                            request.WeekNumber,
                            request.HoursWorked,
                            request.HourlyCompensation,
                            request.AdditionalCompensation
                        );
                        
                        // Return PDF for download
                        var fileName = $"Factuur_Week_{request.WeekNumber}_{request.Year}.pdf";
                        return Results.File(pdfBytes, "application/pdf", fileName);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return ApiResponseFactory.Error(
                            ex.Message,
                            StatusCodes.Status400BadRequest);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error(
                            $"Failed to generate invoice: {ex.Message}",
                            StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}

