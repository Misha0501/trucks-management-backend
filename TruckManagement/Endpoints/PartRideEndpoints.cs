using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TruckManagement.DTOs;
using TruckManagement.Helpers;

public static class PartRideEndpoints
{
    public static void MapPartRideEndpoints(this WebApplication app)
    {
        app.MapPost("/partrides",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
            async (
                [FromBody] CreatePartRideRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (request == null)
                    {
                        return ApiResponseFactory.Error("Request cannot be null.", StatusCodes.Status400BadRequest);
                    }

                    // companyId must be provided and valid
                    if (string.IsNullOrWhiteSpace(request.CompanyId))
                    {
                        return ApiResponseFactory.Error(
                            "companyId is required and cannot be empty.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    if (!Guid.TryParse(request.CompanyId, out Guid companyGuid))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid companyId format.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Validate Start < End
                    if (request.End < request.Start)
                    {
                        return ApiResponseFactory.Error(
                            "'End' time cannot be before 'Start' time.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Attempt to parse other GUIDs
                    Guid? rideGuid = TryParseGuid(request.RideId);
                    Guid? carGuid = TryParseGuid(request.CarId);
                    Guid? driverGuid = TryParseGuid(request.DriverId);
                    Guid? rateGuid = TryParseGuid(request.RateId);
                    Guid? surchargeGuid = TryParseGuid(request.SurchargeId);
                    Guid? charterGuid = TryParseGuid(request.CharterId);
                    Guid? clientGuid = TryParseGuid(request.ClientId);
                    Guid? unitGuid = TryParseGuid(request.UnitId);

                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                    if (!isGlobalAdmin)
                    {
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                        if (contactPerson == null)
                        {
                            return ApiResponseFactory.Error(
                                "No contact person profile found. You are not authorized.",
                                StatusCodes.Status403Forbidden
                            );
                        }

                        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Distinct()
                            .ToList();

                        // Also check any client-based associations
                        var clientIds = contactPerson.ContactPersonClientCompanies
                            .Where(cpc => cpc.ClientId.HasValue)
                            .Select(cpc => cpc?.ClientId.Value)
                            .Distinct()
                            .ToList();

                        var accessibleCompanyIds = associatedCompanyIds
                            .Concat(clientIds)
                            .Distinct()
                            .ToList();

                        // Check if this user is associated with the posted company
                        if (!accessibleCompanyIds.Contains(companyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "You are not authorized to create a PartRide for this company.",
                                StatusCodes.Status403Forbidden
                            );
                        }

                        // If a client is specified, ensure it belongs to the same company
                        if (clientGuid.HasValue)
                        {
                            var clientEntity = await db.Clients
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(c => c.Id == clientGuid.Value);

                            if (clientEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified client does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (clientEntity.CompanyId != companyGuid)
                            {
                                return ApiResponseFactory.Error(
                                    "Client's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        // Similarly, check each optional foreign key
                        if (rideGuid.HasValue)
                        {
                            var rideEntity = await db.Rides
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(r => r.Id == rideGuid.Value);
                            if (rideEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified ride does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                            if (rideEntity.CompanyId != companyGuid)
                            {
                                return ApiResponseFactory.Error(
                                    "Ride's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        if (carGuid.HasValue)
                        {
                            var carEntity = await db.Cars
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(c => c.Id == carGuid.Value);
                            if (carEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified car does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                            if (carEntity.CompanyId != companyGuid)
                            {
                                return ApiResponseFactory.Error(
                                    "Car's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        if (driverGuid.HasValue)
                        {
                            var driverEntity = await db.Drivers
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(d => d.Id == driverGuid.Value);
                            if (driverEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified driver does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                            if (driverEntity.CompanyId.HasValue && driverEntity.CompanyId.Value != companyGuid)
                            {
                                return ApiResponseFactory.Error(
                                    "Driver's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        if (rateGuid.HasValue)
                        {
                            var rateEntity = await db.Rates
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(r => r.Id == rateGuid.Value);
                            if (rateEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified rate does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                            if (rateEntity.CompanyId != companyGuid)
                            {
                                return ApiResponseFactory.Error(
                                    "Rate's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        if (surchargeGuid.HasValue)
                        {
                            var surchargeEntity = await db.Surcharges
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(s => s.Id == surchargeGuid.Value);
                            if (surchargeEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified surcharge does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                            if (surchargeEntity.CompanyId != companyGuid)
                            {
                                return ApiResponseFactory.Error(
                                    "Surcharge's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        if (charterGuid.HasValue)
                        {
                            var charterEntity = await db.Charters
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(c => c.Id == charterGuid.Value);
                            if (charterEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified charter does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                            if (charterEntity.CompanyId != companyGuid)
                            {
                                return ApiResponseFactory.Error(
                                    "Charter's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }
                    }

                    // Calculate decimal hours: (End - Start) - Rest
                    var rawTime = (request.End - request.Start).TotalHours;
                    var decimalHours = rawTime - request.Rest.TotalHours;
                    if (decimalHours < 0)
                    {
                        return ApiResponseFactory.Error(
                            "Computed total hours is negative. Check Start/End/Rest times.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    var newPartRide = new PartRide
                    {
                        Id = Guid.NewGuid(),
                        RideId = rideGuid,
                        Date = request.Date,
                        Start = request.Start,
                        End = request.End,
                        Rest = request.Rest,
                        Kilometers = request.Kilometers,
                        CarId = carGuid,
                        DriverId = driverGuid,
                        Costs = request.Costs,
                        Employer = request.Employer,
                        ClientId = clientGuid,
                        Day = request.Date.Day,
                        // You can also compute week number here, or use request.WeekNumber directly
                        WeekNumber = request.WeekNumber > 0
                            ? request.WeekNumber
                            : GetIso8601WeekOfYear(request.Date),
                        Hours = rawTime,
                        DecimalHours = decimalHours,
                        UnitId = unitGuid,
                        RateId = rateGuid,
                        CostsDescription = request.CostsDescription,
                        SurchargeId = surchargeGuid,
                        Turnover = request.Turnover,
                        Remark = request.Remark,
                        CompanyId = companyGuid,
                        CharterId = charterGuid
                    };

                    db.PartRides.Add(newPartRide);
                    await db.SaveChangesAsync();

                    var responseData = new
                    {
                        newPartRide.Id,
                        newPartRide.Date,
                        newPartRide.Start,
                        newPartRide.End,
                        newPartRide.Rest,
                        newPartRide.Kilometers,
                        newPartRide.Costs,
                        newPartRide.Employer,
                        newPartRide.ClientId,
                        newPartRide.CompanyId,
                        newPartRide.Day,
                        newPartRide.WeekNumber,
                        newPartRide.Hours,
                        newPartRide.DecimalHours,
                        newPartRide.CostsDescription,
                        newPartRide.Turnover,
                        newPartRide.Remark
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status201Created);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error creating PartRide: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while creating the PartRide.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });
        
        
    }

    private static Guid? TryParseGuid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        return Guid.TryParse(input, out var parsed) ? parsed : null;
    }

    // Example of an ISO8601 week calculation
    private static int GetIso8601WeekOfYear(DateTime time)
    {
        // This presumes that weeks start with Monday. 
        // Week 1 is the week that has at least four days in the new year.
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day == DayOfWeek.Sunday)
        {
            time = time.AddDays(-1);
        }

        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            time,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday
        );
    }
}
