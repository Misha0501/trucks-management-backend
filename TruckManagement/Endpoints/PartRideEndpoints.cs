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
using TruckManagement.Utilities;

public static class PartRideEndpoints
{
    public static void MapPartRideEndpoints(this WebApplication app)
    {
        app.MapPost("/partrides",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
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


                    // 2) Convert Start/End to decimal hours
                    double startDecimal = request.Start.TotalHours;
                    double endDecimal = request.End.TotalHours;

                    // SHIFT crosses midnight if end <= start in decimal hours
                    bool crossesMidnight = (endDecimal <= startDecimal);

                    // 3) Validate references/roles once (driver, company, etc.)
                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isDriver = currentUser.IsInRole("driver");

                    var validationResult = await ValidateReferencesAndRolesAsync(
                        db, userManager, currentUser,
                        request, userId, companyGuid,
                        isGlobalAdmin, isDriver
                    );
                    if (validationResult != null)
                    {
                        // if validation fails, it returns an error
                        return validationResult;
                    }

                    // 4) Create either one or two segments
                    var createdPartRides = new List<PartRide>();

                    if (!crossesMidnight)
                    {
                        // Single segment (no midnight crossover)
                        var singleRide = await CreateAndSavePartRideSegment(
                            db, request, companyGuid,
                            request.Date, request.Start, request.End
                        );
                        createdPartRides.Add(singleRide);
                    }
                    else
                    {
                        // SHIFT CROSSING MIDNIGHT => 2 segments

                        // Segment #1: from Start -> 24:00, same date
                        var midnight = TimeSpan.FromHours(24.0);
                        var firstRide = await CreateAndSavePartRideSegment(
                            db, request, companyGuid,
                            request.Date, request.Start, midnight
                        );
                        createdPartRides.Add(firstRide);

                        // Segment #2: from 00:00 -> End, next day
                        var secondRide = await CreateAndSavePartRideSegment(
                            db, request, companyGuid,
                            request.Date.AddDays(1),
                            TimeSpan.Zero,
                            request.End
                        );
                        createdPartRides.Add(secondRide);
                    }

                    // 5) Return everything in one response
                    var responseData = createdPartRides.Select(pr => new
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
                        pr.DriverId,
                        pr.CompanyId,
                        pr.Day,
                        pr.WeekNumber,
                        pr.DecimalHours,
                        pr.CostsDescription,
                        pr.Turnover,
                        pr.Remark
                    }).ToList();

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status201Created);


                    // Convert Start and End time to decimal hours
                    double startTimeDecimal = request.Start.TotalHours;
                    double endTimeDecimal = request.End.TotalHours;

                    // TODO: provide hour code in the request
                    double untaxedAllowanceNormalDayPartial =
                        WorkHoursCalculator.CalculateUntaxedAllowanceNormalDayPartial(
                            startOfShift: startTimeDecimal,
                            endOfShift: endTimeDecimal,
                            dayRateBefore18: 0.77,
                            eveningRateAfter18: 3.52,
                            isHoliday: false
                        );

                    double untaxedAllowanceSingleDay = WorkHoursCalculator.CalculateUntaxedAllowanceSingleDay(
                        hourCode: "Eendaagserit",
                        singleDayTripCode: "Eendaagserit",
                        startTime: startTimeDecimal,
                        endTime: endTimeDecimal,
                        untaxedAllowanceNormalDayPartial: untaxedAllowanceNormalDayPartial,
                        dayRateBefore18: 0.77,
                        eveningRateAfter18: 3.52,
                        lumpSumIf12h: 14.63
                    );

                    double sickHours = WorkHoursCalculator.CalculateSickHours(
                        hourCode: "",
                        holidayName: "",
                        weeklyPercentage: 100.0,
                        startTime: startTimeDecimal,
                        endTime: endTimeDecimal
                    );
                    double holidayHours = WorkHoursCalculator.CalculateHolidayHours(
                        hourCode: "vak",
                        weeklyPercentage: 100.0,
                        startTime: startTimeDecimal,
                        endTime: endTimeDecimal
                    );

                    // TODO: Replace with actual config values (e.g., from settings) 
                    double nightAllowance = NightAllowanceCalculator.CalculateNightAllowance(
                        inputDate: request.Date,
                        startTime: startTimeDecimal,
                        endTime: endTimeDecimal,
                        nightStartTime: 21.0, // Replace with actual config value (e.g., from settings)
                        nightEndTime: 5.0, // Replace with actual config value (e.g., from settings)
                        nightHoursAllowed: true, // Assuming night allowance is enabled
                        nightHours19Percent: false, // Placeholder: replace based on settings
                        nightHoursInEuros: true, // Placeholder: replace based on settings
                        someMonthDate: DateTime.UtcNow, // Placeholder: replace with reference month
                        driverRateOne: 18.71, // Placeholder: replace with actual rate
                        driverRateTwo: 18.71, // Placeholder: replace with actual rate
                        nightAllowanceRate: 0.19, // Placeholder: replace with actual rate from settings
                        nightHoursWholeHours: false // Placeholder: replace based on settings
                    );

                    // Calculate total break using the provided function
                    double totalBreak = WorkHoursCalculator.CalculateTotalBreak(
                        breakScheduleOn: true, // Always true as per your request
                        startTime: startTimeDecimal,
                        endTime: endTimeDecimal,
                        hourCode: "", // Assuming empty as no information provided
                        timeForTimeCode: "tvt", // Assuming empty as no information provided
                        sickHours: sickHours, // Assuming no sick hours
                        holidayHours: holidayHours // Assuming no holiday hours
                    );

                    double totalHours = WorkHoursCalculator.CalculateTotalHours(
                        shiftStart: startTimeDecimal,
                        shiftEnd: endTimeDecimal,
                        breakDuration: totalBreak,
                        manualAdjustment: 0 // TODO: replace with manual Adjustment
                    );
                    
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
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
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
                    // Validate PartRide ID
                    if (!Guid.TryParse(id, out Guid partRideGuid))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid PartRide ID format.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Load the existing PartRide
                    var existingPartRide = await db.PartRides
                        .FirstOrDefaultAsync(pr => pr.Id == partRideGuid);

                    if (existingPartRide == null)
                    {
                        return ApiResponseFactory.Error(
                            "PartRide not found.",
                            StatusCodes.Status404NotFound
                        );
                    }

                    // Retrieve current user's info
                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isDriver = currentUser.IsInRole("driver");

                    // If the user is a driver, ensure they own this PartRide
                    if (isDriver)
                    {
                        // Load the driver entity for this user
                        var driverEntity = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driverEntity == null)
                        {
                            return ApiResponseFactory.Error(
                                "You are not registered as a driver. Contact your administrator.",
                                StatusCodes.Status403Forbidden
                            );
                        }

                        // Check if the PartRide's DriverId matches this driver's ID
                        if (!existingPartRide.DriverId.HasValue ||
                            existingPartRide.DriverId.Value != driverEntity.Id)
                        {
                            return ApiResponseFactory.Error(
                                "Drivers can only edit their own PartRides.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // Determine the new or existing company
                    Guid currentCompanyId = existingPartRide.CompanyId ?? Guid.Empty;

                    // If the request has a new company ID
                    if (!string.IsNullOrWhiteSpace(request.CompanyId))
                    {
                        if (!Guid.TryParse(request.CompanyId, out Guid newCompanyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid companyId format.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        currentCompanyId = newCompanyGuid;
                    }

                    // If not global admin & not driver, we do the contact-person association check
                    if (!isGlobalAdmin && !isDriver)
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

                        // Check if new or existing company is accessible
                        if (currentCompanyId != Guid.Empty && !associatedCompanyIds.Contains(currentCompanyId))
                        {
                            return ApiResponseFactory.Error(
                                "You are not authorized for the specified company.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // Attempt to parse optional references
                    Guid? newRideId = TryParseGuid(request.RideId);
                    Guid? newCarId = TryParseGuid(request.CarId);
                    Guid? newDriverId = TryParseGuid(request.DriverId);
                    Guid? newRateId = TryParseGuid(request.RateId);
                    Guid? newSurchargeId = TryParseGuid(request.SurchargeId);
                    Guid? newCharterId = TryParseGuid(request.CharterId);
                    Guid? newUnitId = TryParseGuid(request.UnitId);
                    Guid? newClientId = TryParseGuid(request.ClientId);

                    // If user is driver, ensure they don't reassign DriverId to someone else
                    if (isDriver && newDriverId.HasValue && newDriverId.Value != existingPartRide.DriverId)
                    {
                        return ApiResponseFactory.Error(
                            "Drivers cannot change the PartRide's DriverId to another user.",
                            StatusCodes.Status403Forbidden
                        );
                    }

                    // If not global admin & not driver, verify references belong to currentCompanyId
                    if (!isGlobalAdmin && !isDriver)
                    {
                        // Validate Ride
                        if (newRideId.HasValue)
                        {
                            var rideEntity = await db.Rides
                                .FirstOrDefaultAsync(r => r.Id == newRideId.Value);
                            if (rideEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified ride does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (rideEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Ride's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        // Validate Car
                        if (newCarId.HasValue)
                        {
                            var carEntity = await db.Cars
                                .FirstOrDefaultAsync(c => c.Id == newCarId.Value);
                            if (carEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified car does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (carEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Car's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        // Validate Driver
                        if (newDriverId.HasValue)
                        {
                            var driverEntity = await db.Drivers
                                .FirstOrDefaultAsync(d => d.Id == newDriverId.Value);
                            if (driverEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified driver does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (driverEntity.CompanyId.HasValue && driverEntity.CompanyId.Value != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Driver's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        // Validate Rate
                        if (newRateId.HasValue)
                        {
                            var rateEntity = await db.Rates
                                .FirstOrDefaultAsync(r => r.Id == newRateId.Value);
                            if (rateEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified rate does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (rateEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Rate's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        // Validate Surcharge
                        if (newSurchargeId.HasValue)
                        {
                            var surchargeEntity = await db.Surcharges
                                .FirstOrDefaultAsync(s => s.Id == newSurchargeId.Value);
                            if (surchargeEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified surcharge does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (surchargeEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Surcharge's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        // Validate Charter
                        if (newCharterId.HasValue)
                        {
                            var charterEntity = await db.Charters
                                .FirstOrDefaultAsync(c => c.Id == newCharterId.Value);
                            if (charterEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified charter does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (charterEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Charter's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }

                        // Validate Client
                        if (newClientId.HasValue)
                        {
                            var clientEntity = await db.Clients
                                .FirstOrDefaultAsync(c => c.Id == newClientId.Value);
                            if (clientEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified client does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            if (clientEntity.CompanyId != currentCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "Client's company does not match the provided companyId.",
                                    StatusCodes.Status400BadRequest
                                );
                            }
                        }
                    }

                    // Update base fields if provided
                    if (request.Date.HasValue) existingPartRide.Date = request.Date.Value;
                    if (request.Start.HasValue) existingPartRide.Start = request.Start.Value;
                    if (request.End.HasValue) existingPartRide.End = request.End.Value;
                    if (request.Rest.HasValue) existingPartRide.Rest = request.Rest.Value;
                    if (request.Kilometers.HasValue) existingPartRide.Kilometers = request.Kilometers.Value;
                    if (request.Costs.HasValue) existingPartRide.Costs = request.Costs.Value;
                    if (!string.IsNullOrWhiteSpace(request.Employer))
                        existingPartRide.Employer = request.Employer;
                    if (request.Day.HasValue) existingPartRide.Day = request.Day.Value;
                    if (request.WeekNumber.HasValue) existingPartRide.WeekNumber = request.WeekNumber.Value;
                    if (request.Turnover.HasValue) existingPartRide.Turnover = request.Turnover.Value;
                    if (!string.IsNullOrWhiteSpace(request.Remark))
                        existingPartRide.Remark = request.Remark;
                    if (!string.IsNullOrWhiteSpace(request.CostsDescription))
                        existingPartRide.CostsDescription = request.CostsDescription;

                    // Update references if new ones are provided
                    if (newRideId.HasValue) existingPartRide.RideId = newRideId;
                    if (newCarId.HasValue) existingPartRide.CarId = newCarId;
                    if (newDriverId.HasValue) existingPartRide.DriverId = newDriverId; // Safe if validated above
                    if (newRateId.HasValue) existingPartRide.RateId = newRateId;
                    if (newSurchargeId.HasValue) existingPartRide.SurchargeId = newSurchargeId;
                    if (newCharterId.HasValue) existingPartRide.CharterId = newCharterId;
                    if (newClientId.HasValue) existingPartRide.ClientId = newClientId;
                    if (newUnitId.HasValue) existingPartRide.UnitId = newUnitId;

                    // If new company is set
                    if (currentCompanyId != Guid.Empty)
                    {
                        existingPartRide.CompanyId = currentCompanyId;
                    }

                    // Recompute Hours & DecimalHours
                    double totalTime = (existingPartRide.End - existingPartRide.Start).TotalHours;
                    double decimalHours = totalTime - existingPartRide.Rest.TotalHours;
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
                        existingPartRide.DecimalHours,
                        existingPartRide.CostsDescription,
                        existingPartRide.Turnover,
                        existingPartRide.Remark,
                        existingPartRide.DriverId
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
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
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
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isDriver = currentUser.IsInRole("driver");

                    // Start with all PartRides
                    IQueryable<PartRide> query = db.PartRides.AsNoTracking();

                    if (!isGlobalAdmin)
                    {
                        // Attempt to load the user’s "Driver" entity
                        Guid? driverGuidOfUser = null;
                        if (isDriver)
                        {
                            // We can add `.IgnoreQueryFilters()` if there's a global filter on drivers
                            var driverEntity = await db.Drivers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                            if (driverEntity != null)
                            {
                                driverGuidOfUser = driverEntity.Id;
                            }
                        }

                        // Attempt to load the user’s "ContactPerson" entity (for other roles)
                        bool isContactPersonRole = currentUser.IsInRole("customerAdmin")
                                                   || currentUser.IsInRole("employer")
                                                   || currentUser.IsInRole("customer")
                                                   || currentUser.IsInRole("customerAccountant");

                        var directCompanyIds = new List<Guid?>();
                        if (isContactPersonRole)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                // if user is purely driver, this might be okay,
                                // but if user is supposed to have contactPerson roles, we throw 403
                                if (!isDriver)
                                {
                                    return ApiResponseFactory.Error(
                                        "No contact person profile found. You are not authorized.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }
                            }
                            else
                            {
                                directCompanyIds = contactPerson.ContactPersonClientCompanies
                                    .Select(cpc => cpc.CompanyId)
                                    .Distinct()
                                    .ToList();
                            }
                        }

                        // Combine conditions: 
                        // If user is a driver only, they see PartRides with their DriverId
                        // If user also has contactPerson roles, they can also see rides from directCompanyIds
                        if (driverGuidOfUser.HasValue)
                        {
                            // Merge driver logic with direct companies
                            // If user is purely driver with no direct companies, directCompanyIds is empty => fallback is driver logic
                            query = query.Where(pr =>
                                (pr.DriverId.HasValue && pr.DriverId.Value == driverGuidOfUser.Value)
                                || (pr.CompanyId.HasValue && directCompanyIds.Contains(pr.CompanyId.Value))
                            );
                        }
                        else
                        {
                            // Not globalAdmin, not driver => must be contactPerson-based
                            query = query.Where(pr =>
                                pr.CompanyId.HasValue && directCompanyIds.Contains(pr.CompanyId.Value)
                            );
                        }
                    }

                    // Apply additional filters from the query string
                    query = ApplyPartRideFilters(
                        query,
                        companyId,
                        clientId,
                        driverId,
                        carId,
                        weekNumber,
                        turnoverMin,
                        turnoverMax,
                        decimalHoursMin,
                        decimalHoursMax
                    );

                    int totalCount = await query.CountAsync();
                    int totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);

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
                            Client = pr.Client != null
                                ? new
                                {
                                    pr.Client.Id,
                                    pr.Client.Name
                                }
                                : null,
                            Company = pr.Company != null
                                ? new
                                {
                                    pr.Company.Id,
                                    pr.Company.Name
                                }
                                : null,
                            pr.Day,
                            pr.WeekNumber,
                            pr.DecimalHours,
                            pr.CostsDescription,
                            pr.Turnover,
                            pr.Remark,
                            Driver = pr.Driver != null
                                ? new
                                {
                                    pr.Driver.Id,
                                    pr.Driver.AspNetUserId
                                }
                                : null,
                            pr.CarId
                        })
                        .ToListAsync();

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

        app.MapGet("/partrides/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    // Validate the PartRide ID
                    if (!Guid.TryParse(id, out Guid partRideGuid))
                    {
                        return ApiResponseFactory.Error("Invalid PartRide ID format.", StatusCodes.Status400BadRequest);
                    }

                    // Find the PartRide
                    var partRide = await db.PartRides
                        .AsNoTracking()
                        .Include(pr => pr.Driver)
                        .ThenInclude(d => d.User)
                        .Include(pr => pr.Company)
                        .Include(pr => pr.Client)
                        .FirstOrDefaultAsync(pr => pr.Id == partRideGuid);

                    if (partRide == null)
                    {
                        return ApiResponseFactory.Error("PartRide not found.", StatusCodes.Status404NotFound);
                    }

                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isDriver = currentUser.IsInRole("driver");

                    // If user is global admin, they can see all part rides
                    if (!isGlobalAdmin)
                    {
                        // If user is a driver, they can only view if DriverId == their own
                        if (isDriver)
                        {
                            var driverEntity = await db.Drivers
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                            if (driverEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not registered as a driver. Contact your administrator.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Check if the PartRide's DriverId matches this driver's ID
                            if (!partRide.DriverId.HasValue || partRide.DriverId.Value != driverEntity.Id)
                            {
                                return ApiResponseFactory.Error(
                                    "Drivers can only view their own PartRides.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                        else
                        {
                            // If user is not driver nor global admin, they must be contact-person-based (customerAdmin, employer, customer, customerAccountant)
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

                            // Check if the PartRide belongs to a company the user is associated with
                            if (!partRide.CompanyId.HasValue ||
                                !associatedCompanyIds.Contains(partRide.CompanyId.Value))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to view this PartRide.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                    }

                    // Build the response
                    var responseData = new
                    {
                        partRide.Id,
                        partRide.Date,
                        partRide.Start,
                        partRide.End,
                        partRide.Rest,
                        partRide.Kilometers,
                        partRide.Costs,
                        partRide.Employer,
                        Client = partRide.Client != null
                            ? new
                            {
                                partRide.Client.Id,
                                partRide.Client.Name
                            }
                            : null,
                        Company = partRide.Company != null
                            ? new
                            {
                                partRide.Company.Id,
                                partRide.Company.Name
                            }
                            : null,
                        partRide.Day,
                        partRide.WeekNumber,
                        partRide.DecimalHours,
                        partRide.CostsDescription,
                        partRide.Turnover,
                        partRide.Remark,
                        Driver = partRide.Driver != null
                            ? new
                            {
                                partRide.Driver.Id,
                                partRide.Driver.AspNetUserId
                            }
                            : null,
                        partRide.CarId,
                        partRide.RateId,
                        partRide.SurchargeId,
                        partRide.CharterId,
                        partRide.UnitId,
                        partRide.RideId
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error fetching PartRide detail: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while retrieving the PartRide detail.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        app.MapDelete("/partrides/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant, driver")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    // Validate PartRide ID
                    if (!Guid.TryParse(id, out Guid partRideGuid))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid PartRide ID format.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Load existing PartRide
                    var existingPartRide = await db.PartRides
                        .FirstOrDefaultAsync(pr => pr.Id == partRideGuid);

                    if (existingPartRide == null)
                    {
                        return ApiResponseFactory.Error("PartRide not found.", StatusCodes.Status404NotFound);
                    }

                    // Get current user info
                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isDriver = currentUser.IsInRole("driver");

                    // If user is global admin, can delete any PartRide
                    if (!isGlobalAdmin)
                    {
                        // If user is driver, can only delete if the PartRide belongs to them
                        if (isDriver)
                        {
                            var driverEntity = await db.Drivers
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);
                            if (driverEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not registered as a driver. Contact your administrator.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            if (!existingPartRide.DriverId.HasValue ||
                                existingPartRide.DriverId.Value != driverEntity.Id)
                            {
                                return ApiResponseFactory.Error(
                                    "Drivers can only delete their own PartRides.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                        else
                        {
                            // Otherwise, user is a contact-person role (customerAdmin, employer, etc.)
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

                            // Gather user's associated companies
                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            // PartRide must belong to one of these companies
                            if (!existingPartRide.CompanyId.HasValue ||
                                !associatedCompanyIds.Contains(existingPartRide.CompanyId.Value))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to delete this PartRide.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                    }

                    // If there's a need to ensure no child references exist,
                    // or handle soft-deletion logic, you can do it here.
                    // For now, assume direct removal is fine.

                    db.PartRides.Remove(existingPartRide);
                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(
                        "PartRide deleted successfully.",
                        StatusCodes.Status200OK
                    );
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error deleting PartRide: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while deleting the PartRide.",
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

    private static IQueryable<PartRide> ApplyPartRideFilters(
        IQueryable<PartRide> query,
        string? companyId,
        string? clientId,
        string? driverId,
        string? carId,
        int? weekNumber,
        decimal? turnoverMin,
        decimal? turnoverMax,
        double? decimalHoursMin,
        double? decimalHoursMax
    )
    {
        if (!string.IsNullOrWhiteSpace(companyId) && Guid.TryParse(companyId, out var companyGuid))
        {
            query = query.Where(pr => pr.CompanyId == companyGuid);
        }

        if (!string.IsNullOrWhiteSpace(clientId) && Guid.TryParse(clientId, out var clientGuid))
        {
            query = query.Where(pr => pr.ClientId == clientGuid);
        }

        if (!string.IsNullOrWhiteSpace(driverId) && Guid.TryParse(driverId, out var driverGuid))
        {
            query = query.Where(pr => pr.DriverId == driverGuid);
        }

        if (!string.IsNullOrWhiteSpace(carId) && Guid.TryParse(carId, out var carGuid))
        {
            query = query.Where(pr => pr.CarId == carGuid);
        }

        if (weekNumber.HasValue && weekNumber.Value > 0)
        {
            query = query.Where(pr => pr.WeekNumber == weekNumber.Value);
        }

        if (turnoverMin.HasValue)
        {
            query = query.Where(pr => pr.Turnover >= turnoverMin.Value);
        }

        if (turnoverMax.HasValue)
        {
            query = query.Where(pr => pr.Turnover <= turnoverMax.Value);
        }

        if (decimalHoursMin.HasValue)
        {
            query = query.Where(pr => pr.DecimalHours >= decimalHoursMin.Value);
        }

        if (decimalHoursMax.HasValue)
        {
            query = query.Where(pr => pr.DecimalHours <= decimalHoursMax.Value);
        }

        return query;
    }

    /// <summary>
    /// Validate references/roles once. If anything invalid => return IResult error,
    /// otherwise null means valid.
    /// 
    /// Replicates your existing logic for checking driver, contact person, etc.
    /// </summary>
    private static async Task<IResult?> ValidateReferencesAndRolesAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal currentUser,
        CreatePartRideRequest request,
        string userId,
        Guid companyGuid,
        bool isGlobalAdmin,
        bool isDriver
    )
    {
        // Attempt to parse other GUIDs
        Guid? rideGuid = TryParseGuid(request.RideId);
        Guid? carGuid = TryParseGuid(request.CarId);
        Guid? driverGuid = TryParseGuid(request.DriverId);
        Guid? rateGuid = TryParseGuid(request.RateId);
        Guid? surchargeGuid = TryParseGuid(request.SurchargeId);
        Guid? charterGuid = TryParseGuid(request.CharterId);
        Guid? clientGuid = TryParseGuid(request.ClientId);
        Guid? unitGuid = TryParseGuid(request.UnitId);

        // If the user is a driver => ensure the driver matches the user & company
        if (isDriver)
        {
            var driverEntity = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId);
            if (driverEntity == null)
            {
                return ApiResponseFactory.Error(
                    "You are not registered as a driver. Contact your administrator.",
                    StatusCodes.Status403Forbidden
                );
            }

            // Must create PartRide for themselves
            if (!driverGuid.HasValue || driverGuid.Value != driverEntity.Id)
            {
                return ApiResponseFactory.Error(
                    "Drivers can only create PartRides for themselves.",
                    StatusCodes.Status403Forbidden
                );
            }

            // Must match same company
            if (!driverEntity.CompanyId.HasValue || driverEntity.CompanyId.Value != companyGuid)
            {
                return ApiResponseFactory.Error(
                    "You are not associated with this company.",
                    StatusCodes.Status403Forbidden
                );
            }
        }

        // If not global admin nor driver => must be contact-person-based
        if (!isGlobalAdmin && !isDriver)
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

            if (!associatedCompanyIds.Contains(companyGuid))
            {
                return ApiResponseFactory.Error(
                    "You are not authorized to create a PartRide for this company.",
                    StatusCodes.Status403Forbidden
                );
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

        return null;
    }
    
    /// <summary>
        /// Creates and saves a single PartRide segment. 
        /// If you want to run calculations (e.g. breaks, night allowance), do it here.
        /// </summary>
        private static async Task<PartRide> CreateAndSavePartRideSegment(
            ApplicationDbContext db,
            CreatePartRideRequest request,
            Guid companyGuid,
            DateTime segmentDate,
            TimeSpan segmentStart,
            TimeSpan segmentEnd
        )
        {
            // Basic shift length
            double rawTime = (segmentEnd - segmentStart).TotalHours; 
            double decimalHours = rawTime - request.Rest.TotalHours;

            // Build the PartRide
            var partRide = new PartRide
            {
                Id = Guid.NewGuid(),
                Date = segmentDate,
                Start = segmentStart,
                End   = segmentEnd,
                Rest  = request.Rest,
                Kilometers = request.Kilometers,
                CarId      = TryParseGuid(request.CarId),
                DriverId   = TryParseGuid(request.DriverId),
                Costs      = request.Costs,
                Employer   = request.Employer,
                ClientId   = TryParseGuid(request.ClientId),
                Day        = segmentDate.Day,
                WeekNumber = request.WeekNumber > 0
                    ? request.WeekNumber
                    : GetIso8601WeekOfYear(segmentDate),
                DecimalHours = decimalHours,
                UnitId = TryParseGuid(request.UnitId),
                RateId = TryParseGuid(request.RateId),
                CostsDescription = request.CostsDescription,
                SurchargeId = TryParseGuid(request.SurchargeId),
                Turnover    = request.Turnover,
                Remark      = request.Remark,
                CompanyId   = companyGuid,
                CharterId   = TryParseGuid(request.CharterId),
                RideId      = TryParseGuid(request.RideId)
            };

            // Optionally run your calculations (WorkHoursCalculator, etc.)
            // e.g. double totalBreak = WorkHoursCalculator.CalculateTotalBreak(...);
            // partRide.Rest = TimeSpan.FromHours(totalBreak);
            
            // e.g. double nightAllowance = NightAllowanceCalculator.CalculateNightAllowance(...);
            // partRide.Turnover += nightAllowance;

            // Save
            db.PartRides.Add(partRide);
            await db.SaveChangesAsync();

            return partRide;
        }
    
}