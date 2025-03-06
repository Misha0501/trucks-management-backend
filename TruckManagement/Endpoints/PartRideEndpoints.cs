using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
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

        app.MapPut("/partrides/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
            async (
                string id,
                [FromBody] UpdatePartRideRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (!Guid.TryParse(id, out Guid partRideGuid))
                    {
                        return ApiResponseFactory.Error("Invalid PartRide ID format.", StatusCodes.Status400BadRequest);
                    }

                    // Load the existing PartRide
                    var existingPartRide = await db.PartRides
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(pr => pr.Id == partRideGuid);

                    if (existingPartRide == null)
                    {
                        return ApiResponseFactory.Error("PartRide not found.", StatusCodes.Status404NotFound);
                    }

                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                    // Make sure we have a valid company ID in the request or keep the existing
                    Guid currentCompanyId = existingPartRide.CompanyId.HasValue
                        ? existingPartRide.CompanyId.Value
                        : Guid.Empty;

                    // If the request has a new company ID, parse it
                    if (!string.IsNullOrWhiteSpace(request.CompanyId))
                    {
                        if (!Guid.TryParse(request.CompanyId, out var newCompanyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid companyId format in the request.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        currentCompanyId = newCompanyGuid;
                    }

                    // If not global admin, verify the user can access that company
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

                        var directCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Distinct()
                            .ToList();

                        var clientIds = contactPerson.ContactPersonClientCompanies
                            .Where(cpc => cpc.ClientId.HasValue)
                            .Select(cpc => cpc?.ClientId.Value)
                            .Distinct()
                            .ToList();

                        var accessibleCompanyIds = directCompanyIds
                            .Concat(clientIds)
                            .Distinct()
                            .ToList();

                        // If the user changes or sets a new companyId, check if in accessible
                        if (currentCompanyId != Guid.Empty && !accessibleCompanyIds.Contains(currentCompanyId))
                        {
                            return ApiResponseFactory.Error(
                                "You are not authorized for the specified company.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // Validate & update optional references
                    Guid? newRideId = TryParseGuid(request.RideId);
                    Guid? newCarId = TryParseGuid(request.CarId);
                    Guid? newDriverId = TryParseGuid(request.DriverId);
                    Guid? newRateId = TryParseGuid(request.RateId);
                    Guid? newSurchargeId = TryParseGuid(request.SurchargeId);
                    Guid? newCharterId = TryParseGuid(request.CharterId);
                    Guid? newUnitId = TryParseGuid(request.UnitId);
                    Guid? newClientId = TryParseGuid(request.ClientId);

                    // If not global admin, ensure each new reference belongs to the same company
                    if (!isGlobalAdmin)
                    {
                        // For each reference, load the entity and check its company ID
                        if (newRideId.HasValue)
                        {
                            var rideEntity = await db.Rides.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(r => r.Id == newRideId.Value);
                            if (rideEntity == null)
                            {
                                return ApiResponseFactory.Error("Ride does not exist.",
                                    StatusCodes.Status400BadRequest);
                            }

                            if (rideEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error("Ride's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest);
                            }
                        }

                        if (newCarId.HasValue)
                        {
                            var carEntity = await db.Cars.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(c => c.Id == newCarId.Value);
                            if (carEntity == null)
                            {
                                return ApiResponseFactory.Error("Car does not exist.", StatusCodes.Status400BadRequest);
                            }

                            if (carEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error("Car's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest);
                            }
                        }

                        if (newDriverId.HasValue)
                        {
                            var driverEntity = await db.Drivers.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(d => d.Id == newDriverId.Value);
                            if (driverEntity == null)
                            {
                                return ApiResponseFactory.Error("Driver does not exist.",
                                    StatusCodes.Status400BadRequest);
                            }

                            if (driverEntity.CompanyId.HasValue && driverEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Driver's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest);
                            }
                        }

                        if (newRateId.HasValue)
                        {
                            var rateEntity = await db.Rates.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(r => r.Id == newRateId.Value);
                            if (rateEntity == null)
                            {
                                return ApiResponseFactory.Error("Rate does not exist.",
                                    StatusCodes.Status400BadRequest);
                            }

                            if (rateEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error("Rate's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest);
                            }
                        }

                        if (newSurchargeId.HasValue)
                        {
                            var surchargeEntity = await db.Surcharges.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(s => s.Id == newSurchargeId.Value);
                            if (surchargeEntity == null)
                            {
                                return ApiResponseFactory.Error("Surcharge does not exist.",
                                    StatusCodes.Status400BadRequest);
                            }

                            if (surchargeEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Surcharge's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest);
                            }
                        }

                        if (newCharterId.HasValue)
                        {
                            var charterEntity = await db.Charters.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(c => c.Id == newCharterId.Value);
                            if (charterEntity == null)
                            {
                                return ApiResponseFactory.Error("Charter does not exist.",
                                    StatusCodes.Status400BadRequest);
                            }

                            if (charterEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Charter's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest);
                            }
                        }

                        if (newClientId.HasValue)
                        {
                            var clientEntity = await db.Clients.IgnoreQueryFilters()
                                .FirstOrDefaultAsync(cl => cl.Id == newClientId.Value);
                            if (clientEntity == null)
                            {
                                return ApiResponseFactory.Error("Client does not exist.",
                                    StatusCodes.Status400BadRequest);
                            }

                            if (clientEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Client's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest);
                            }
                        }
                    }

                    // Update base fields only if provided
                    if (request.Date.HasValue) existingPartRide.Date = request.Date.Value;
                    if (request.Start.HasValue) existingPartRide.Start = request.Start.Value;
                    if (request.End.HasValue) existingPartRide.End = request.End.Value;
                    if (request.Rest.HasValue) existingPartRide.Rest = request.Rest.Value;
                    if (request.Kilometers.HasValue) existingPartRide.Kilometers = request.Kilometers.Value;
                    if (request.Costs.HasValue) existingPartRide.Costs = request.Costs.Value;
                    if (!string.IsNullOrWhiteSpace(request.Employer)) existingPartRide.Employer = request.Employer;
                    if (request.Day.HasValue) existingPartRide.Day = request.Day.Value;
                    if (request.WeekNumber.HasValue) existingPartRide.WeekNumber = request.WeekNumber.Value;
                    if (request.Turnover.HasValue) existingPartRide.Turnover = request.Turnover.Value;
                    if (!string.IsNullOrWhiteSpace(request.Remark)) existingPartRide.Remark = request.Remark;
                    if (!string.IsNullOrWhiteSpace(request.CostsDescription))
                        existingPartRide.CostsDescription = request.CostsDescription;

                    // If references are updated, set them
                    if (newRideId.HasValue) existingPartRide.RideId = newRideId;
                    if (newCarId.HasValue) existingPartRide.CarId = newCarId;
                    if (newDriverId.HasValue) existingPartRide.DriverId = newDriverId;
                    if (newRateId.HasValue) existingPartRide.RateId = newRateId;
                    if (newSurchargeId.HasValue) existingPartRide.SurchargeId = newSurchargeId;
                    if (newCharterId.HasValue) existingPartRide.CharterId = newCharterId;
                    if (newClientId.HasValue) existingPartRide.ClientId = newClientId;
                    if (newUnitId.HasValue) existingPartRide.UnitId = newUnitId;

                    // If new company is set, update
                    if (currentCompanyId != Guid.Empty)
                    {
                        existingPartRide.CompanyId = currentCompanyId;
                    }

                    // Recalculate Hours & DecimalHours if Start/End/Rest changed
                    var totalTime = (existingPartRide.End - existingPartRide.Start).TotalHours;
                    var decimalHours = totalTime - existingPartRide.Rest.TotalHours;
                    existingPartRide.Hours = totalTime;
                    existingPartRide.DecimalHours = decimalHours;

                    await db.SaveChangesAsync();

                    var responseData = new
                    {
                        existingPartRide.Id,
                        existingPartRide.Date,
                        existingPartRide.Start,
                        existingPartRide.End,
                        existingPartRide.Rest,
                        existingPartRide.Kilometers,
                        existingPartRide.Costs,
                        existingPartRide.Employer,
                        existingPartRide.ClientId,
                        existingPartRide.CompanyId,
                        existingPartRide.Day,
                        existingPartRide.WeekNumber,
                        existingPartRide.Hours,
                        existingPartRide.DecimalHours,
                        existingPartRide.CostsDescription,
                        existingPartRide.Turnover,
                        existingPartRide.Remark
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error updating PartRide: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while updating the PartRide.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        // GET /partrides?companyId=&clientId=&driverId=&carId=&weekNumber=&turnoverMin=&turnoverMax=&decimalHoursMin=&decimalHoursMax=&pageNumber=1&pageSize=10
        app.MapGet("/partrides",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
            async (
                [FromQuery] string? companyId,
                [FromQuery] string? clientId,
                [FromQuery] string? driverId,
                [FromQuery] string? carId,
                [FromQuery] int? weekNumber,
                [FromQuery] decimal? turnoverMin,
                [FromQuery] decimal? turnoverMax,
                [FromQuery] double? decimalHoursMin,
                [FromQuery] double? decimalHoursMax,
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

                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                    // Start the base query
                    IQueryable<PartRide> query = db.PartRides.AsNoTracking();

                    // If not global admin, restrict to user’s accessible companies
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

                        var directCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Select(cpc => cpc.CompanyId)
                            .Distinct()
                            .ToList();
                        
                        // Filter the query by accessible companies
                        query = query.Where(pr =>
                            pr.CompanyId.HasValue && directCompanyIds.Contains(pr.CompanyId.Value));
                    }

                    // Now apply optional filters
                    // companyId
                    if (!string.IsNullOrWhiteSpace(companyId) && Guid.TryParse(companyId, out var companyGuid))
                    {
                        query = query.Where(pr => pr.CompanyId == companyGuid);
                    }

                    // clientId
                    if (!string.IsNullOrWhiteSpace(clientId) && Guid.TryParse(clientId, out var clientGuid))
                    {
                        query = query.Where(pr => pr.ClientId == clientGuid);
                    }

                    // driverId
                    if (!string.IsNullOrWhiteSpace(driverId) && Guid.TryParse(driverId, out var driverGuid))
                    {
                        query = query.Where(pr => pr.DriverId == driverGuid);
                    }

                    // carId
                    if (!string.IsNullOrWhiteSpace(carId) && Guid.TryParse(carId, out var carGuid))
                    {
                        query = query.Where(pr => pr.CarId == carGuid);
                    }

                    // weekNumber
                    if (weekNumber.HasValue && weekNumber.Value > 0)
                    {
                        query = query.Where(pr => pr.WeekNumber == weekNumber.Value);
                    }

                    // turnoverMin
                    if (turnoverMin.HasValue)
                    {
                        query = query.Where(pr => pr.Turnover >= turnoverMin.Value);
                    }

                    // turnoverMax
                    if (turnoverMax.HasValue)
                    {
                        query = query.Where(pr => pr.Turnover <= turnoverMax.Value);
                    }

                    // decimalHoursMin
                    if (decimalHoursMin.HasValue)
                    {
                        query = query.Where(pr => pr.DecimalHours >= decimalHoursMin.Value);
                    }

                    // decimalHoursMax
                    if (decimalHoursMax.HasValue)
                    {
                        query = query.Where(pr => pr.DecimalHours <= decimalHoursMax.Value);
                    }

                    // Count total
                    var totalCount = await query.CountAsync();

                    // Apply pagination
                    var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);

                    // Fetch data
                    var partRides = await query
                        .OrderByDescending(pr => pr.Date)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(pr => new
                        {
                            pr.Id,
                            pr.Date,
                            pr.Start,
                            pr.End,
                            pr.Rest,
                            pr.Kilometers,
                            pr.Costs,
                            pr.Employer,
                            pr.ClientId,
                            pr.CompanyId,
                            pr.Day,
                            pr.WeekNumber,
                            pr.Hours,
                            pr.DecimalHours,
                            pr.CostsDescription,
                            pr.Turnover,
                            pr.Remark
                        })
                        .ToListAsync();

                    // Build response
                    var responseData = new
                    {
                        totalCount,
                        totalPages,
                        pageNumber,
                        pageSize,
                        data = partRides
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error fetching PartRides: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while retrieving part rides.",
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
        var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day == DayOfWeek.Sunday)
        {
            time = time.AddDays(-1);
        }

        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            time,
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday
        );
    }
}