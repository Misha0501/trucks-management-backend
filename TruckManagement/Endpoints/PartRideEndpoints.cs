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
                        pr.Remark,
                        pr.CorrectionTotalHours,
                        pr.TaxFreeCompensation,
                        pr.StandOver,
                        pr.NightAllowance,
                        pr.KilometerReimbursement,
                        pr.ExtraKilometers,
                        pr.ConsignmentFee,
                        pr.SaturdayHours,
                        pr.SundayHolidayHours,
                        pr.VariousCompensation,
                        pr.HoursOptionId,
                        HoursOption = pr.HoursOption != null ? pr.HoursOption.Name : null,
                        pr.HoursCodeId,
                        HoursCode = pr.HoursCode != null ? pr.HoursCode.Name : null
                    }).ToList();

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

                    // Update base fields if provided
                    if (request.Date.HasValue) existingPartRide.Date = request.Date.Value;
                    if (request.Start.HasValue) existingPartRide.Start = request.Start.Value;
                    if (request.End.HasValue) existingPartRide.End = request.End.Value;
                    if (request.Kilometers.HasValue) existingPartRide.Kilometers = request.Kilometers.Value;
                    if (request.Costs.HasValue) existingPartRide.Costs = request.Costs.Value;
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

                    // 13) Check if updated End crosses midnight relative to Start
                    double startTimeDecimal = existingPartRide.Start.TotalHours;
                    double endTimeDecimal = existingPartRide.End.TotalHours;
                    bool crossesMidnight = (endTimeDecimal <= startTimeDecimal);

                    if (!crossesMidnight)
                    {
                        // 13a) If NOT crossing midnight, just recalc & save
                        RecalculatePartRideValues(existingPartRide);
                        await db.SaveChangesAsync();

                        // Return single updated PartRide
                        var responseSingle = ToResponsePartRide(existingPartRide);
                        return ApiResponseFactory.Success(responseSingle, StatusCodes.Status200OK);
                    }
                    else
                    {
                        // 13b) SHIFT CROSSES MIDNIGHT
                        // Keep existing from Start -> 24:00
                        existingPartRide.End = TimeSpan.FromHours(24);
                        RecalculatePartRideValues(existingPartRide);

                        // Create NEW PartRide for [00:00 -> new End], date is +1 day
                        var newPartRide = new PartRide
                        {
                            // copy relevant fields from existing
                            CompanyId = existingPartRide.CompanyId,
                            DriverId = existingPartRide.DriverId,
                            CarId = existingPartRide.CarId,
                            RideId = existingPartRide.RideId,
                            RateId = existingPartRide.RateId,
                            SurchargeId = existingPartRide.SurchargeId,
                            CharterId = existingPartRide.CharterId,
                            ClientId = existingPartRide.ClientId,
                            UnitId = existingPartRide.UnitId,

                            // date is next day
                            Date = existingPartRide.Date.AddDays(1),
                            Start = TimeSpan.FromHours(0),
                            End = TimeSpan.FromHours(endTimeDecimal),
                            Kilometers = existingPartRide.Kilometers,
                            Costs = existingPartRide.Costs,
                            Employer = existingPartRide.Employer,
                            Day = existingPartRide.Day, // or compute anew
                            WeekNumber = existingPartRide.WeekNumber, // or compute anew
                            Turnover = existingPartRide.Turnover,
                            Remark = existingPartRide.Remark,
                            CostsDescription = existingPartRide.CostsDescription
                        };

                        // Recalculate new PartRide
                        RecalculatePartRideValues(newPartRide);

                        // Add and save both
                        await db.PartRides.AddAsync(newPartRide);
                        await db.SaveChangesAsync();

                        // Return both PartRides in the response
                        var responseData = new
                        {
                            ExistingPartRide = ToResponsePartRide(existingPartRide),
                            NewSegment = ToResponsePartRide(newPartRide)
                        };
                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
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
                            pr.CarId,
                            pr.CorrectionTotalHours,
                            pr.TaxFreeCompensation,
                            pr.StandOver,
                            pr.NightAllowance,
                            pr.KilometerReimbursement,
                            pr.ExtraKilometers,
                            pr.ConsignmentFee,
                            pr.SaturdayHours,
                            pr.SundayHolidayHours,
                            pr.VariousCompensation,
                            HoursOption = pr.HoursOption != null
                                ? new
                                {
                                    pr.HoursOption.Id,
                                    pr.HoursOption.Name
                                }
                                : null,
                            HoursCode = pr.HoursCode != null
                                ? new
                                {
                                    pr.HoursCode.Id,
                                    pr.HoursCode.Name
                                }
                                : null
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
                        .Include(pr => pr.Client)
                        .Include(pr => pr.Car)
                        .Include(pr => pr.Rate)
                        .Include(pr => pr.Surcharge)
                        .Include(pr => pr.Charter)
                        .Include(pr => pr.Unit)
                        .Include(pr => pr.Ride)
                        .Include(pr => pr.HoursOption)
                        .Include(pr => pr.HoursCode)
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
                        Driver = partRide.Driver != null
                            ? new
                            {
                                partRide.Driver.Id,
                                partRide.Driver.AspNetUserId,
                                partRide.Driver.User.FirstName,
                                partRide.Driver.User.LastName,
                            }
                            : null,
                        Car = partRide.Car != null
                            ? new
                            {
                                partRide.Car.Id,
                                partRide.Car.LicensePlate
                            }
                            : null,
                        Rate = partRide.Rate != null
                            ? new
                            {
                                partRide.Rate.Id,
                                partRide.Rate.Name
                            }
                            : null,
                        Surcharge = partRide.Surcharge != null
                            ? new
                            {
                                partRide.Surcharge.Id,
                                partRide.Surcharge.Value
                            }
                            : null,
                        Charter = partRide.Charter != null
                            ? new
                            {
                                partRide.Charter.Id,
                                partRide.Charter.Name
                            }
                            : null,
                        Unit = partRide.Unit != null
                            ? new
                            {
                                partRide.Unit.Id,
                                partRide.Unit.Value
                            }
                            : null,
                        Ride = partRide.Ride != null
                            ? new
                            {
                                partRide.Ride.Id,
                                partRide.Ride.Name
                            }
                            : null,
                        HoursOption = partRide.HoursOption != null
                            ? new
                            {
                                partRide.HoursOption.Id,
                                partRide.HoursOption.Name
                            }
                            : null,
                        HoursCode = partRide.HoursCode != null
                            ? new
                            {
                                partRide.HoursCode.Id,
                                partRide.HoursCode.Name
                            }
                            : null,
                        partRide.Day,
                        partRide.WeekNumber,
                        partRide.DecimalHours,
                        partRide.CostsDescription,
                        partRide.Turnover,
                        partRide.Remark,
                        partRide.CorrectionTotalHours,
                        partRide.TaxFreeCompensation,
                        partRide.StandOver,
                        partRide.NightAllowance,
                        partRide.KilometerReimbursement,
                        partRide.ExtraKilometers,
                        partRide.ConsignmentFee,
                        partRide.SaturdayHours,
                        partRide.SundayHolidayHours,
                        partRide.VariousCompensation
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

    private static void RecalculatePartRideValues(PartRide partRide)
    {
        // Recompute time calculations based on updated Start/End/Rest
        double startTimeDecimal = partRide.Start.TotalHours;
        double endTimeDecimal = partRide.End.TotalHours;

        // Calculate additional allowances and totals using helper functions
        double untaxedAllowanceNormalDayPartial =
            WorkHoursCalculator.CalculateUntaxedAllowanceNormalDayPartial(
                startOfShift: startTimeDecimal,
                endOfShift: endTimeDecimal,
                isHoliday: false
            );

        double untaxedAllowanceSingleDay = WorkHoursCalculator.CalculateUntaxedAllowanceSingleDay(
            hourCode: "Eendaagserit",
            singleDayTripCode: "Eendaagserit",
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            untaxedAllowanceNormalDayPartial: untaxedAllowanceNormalDayPartial,
            lumpSumIf12h: 14.63
        );

        double calculatedSickHours = WorkHoursCalculator.CalculateSickHours(
            hourCode: "",
            holidayName: "",
            weeklyPercentage: 100.0,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double calculatedHolidayHours = WorkHoursCalculator.CalculateHolidayHours(
            hourCode: "vak",
            weeklyPercentage: 100.0,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double calculatedNightAllowance = NightAllowanceCalculator.CalculateNightAllowance(
            inputDate: partRide.Date,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            nightHoursAllowed: true,
            nightHours19Percent: false,
            nightHoursInEuros: true,
            someMonthDate: DateTime.UtcNow,
            driverRateOne: 18.71,
            driverRateTwo: 18.71,
            nightAllowanceRate: 0.19,
            nightHoursWholeHours: false
        );

        double totalBreak = WorkHoursCalculator.CalculateTotalBreak(
            breakScheduleOn: true,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            hourCode: "",
            timeForTimeCode: "tvt",
            sickHours: calculatedSickHours,
            holidayHours: calculatedHolidayHours
        );

        double totalHoursCalculated = WorkHoursCalculator.CalculateTotalHours(
            shiftStart: startTimeDecimal,
            shiftEnd: endTimeDecimal,
            breakDuration: totalBreak,
            manualAdjustment: 0
        );

        partRide.DecimalHours = totalHoursCalculated;
        partRide.CorrectionTotalHours = 0; // Set to 0 or use your own logic
        partRide.TaxFreeCompensation = untaxedAllowanceSingleDay;
        partRide.NightAllowance = calculatedNightAllowance;
        partRide.StandOver = 0.0;
        partRide.KilometerReimbursement = 0.0;
        partRide.ExtraKilometers = 0.0;
        partRide.ConsignmentFee = 0.0;
        partRide.SaturdayHours = 0.0;
        partRide.SundayHolidayHours = 0.0;
        partRide.VariousCompensation = 0.0;
    }

    private static object ToResponsePartRide(PartRide pr)
    {
        return new
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
            pr.DecimalHours,
            pr.CostsDescription,
            pr.Turnover,
            pr.Remark,
            pr.DriverId,
            pr.CorrectionTotalHours,
            pr.TaxFreeCompensation,
            pr.NightAllowance,
            pr.StandOver,
            pr.KilometerReimbursement,
            pr.ExtraKilometers,
            pr.ConsignmentFee,
            pr.SaturdayHours,
            pr.SundayHolidayHours,
            pr.VariousCompensation,
        };
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
        // 1) Convert Start/End to decimal hours
        double startTimeDecimal = segmentStart.TotalHours;
        double endTimeDecimal = segmentEnd.TotalHours;

        // 2) Compute base shift length ignoring "Rest" from the request
        double rawTime = (endTimeDecimal - startTimeDecimal);
        // If you allow crossing midnight in these segments, rawTime could be negative. 
        // Typically you'd handle that outside or split into two segments.

        // 3) Some basic calculations from your request:
        //    (If you prefer reading from request directly, feel free.)

        double weeklyPercentage = 100.0; // e.g. user is full-time
        bool isHoliday = false; // or check your own holiday logic

        // 4) RUN THE CALCULATIONS:

        // 4a) Untaxed allowances
        double untaxedAllowanceNormalDayPartial = WorkHoursCalculator.CalculateUntaxedAllowanceNormalDayPartial(
            startOfShift: startTimeDecimal,
            endOfShift: endTimeDecimal,
            isHoliday: isHoliday
        );

        double untaxedAllowanceSingleDay = WorkHoursCalculator.CalculateUntaxedAllowanceSingleDay(
            hourCode: "Eendaagserit", // Example placeholders 
            singleDayTripCode: "Eendaagserit", // Example 
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            untaxedAllowanceNormalDayPartial: untaxedAllowanceNormalDayPartial,
            lumpSumIf12h: 14.63 // Example lumpsum
        );

        // 4b) Sick/Holiday hours
        double sickHours = WorkHoursCalculator.CalculateSickHours(
            hourCode: "", // Replace with real hour code from request
            holidayName: "", // Or request.HolidayName if you have it
            weeklyPercentage: weeklyPercentage,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );
        double holidayHours = WorkHoursCalculator.CalculateHolidayHours(
            hourCode: "vak", // Replace with real hour code from request
            weeklyPercentage: weeklyPercentage,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        // 4c) Night allowance
        double nightAllowance = NightAllowanceCalculator.CalculateNightAllowance(
            inputDate: segmentDate,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            nightHoursAllowed: true,
            nightHours19Percent: false,
            nightHoursInEuros: true,
            someMonthDate: DateTime.UtcNow,
            driverRateOne: 18.71,
            driverRateTwo: 18.71,
            nightAllowanceRate: 0.19,
            nightHoursWholeHours: false
        );

        // 4d) Break calculation (TotalBreak)
        double totalBreak = WorkHoursCalculator.CalculateTotalBreak(
            breakScheduleOn: true, // Always true
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            hourCode: "", // or request.HourCode if you have it
            timeForTimeCode: "tvt", // Example 
            sickHours: sickHours,
            holidayHours: holidayHours
        );

        // 4e) Compute final “totalHours” after subtracting the break, plus any manual adjustment
        double manualAdjustment = 0.0; // or request.ManualAdjustment if you have that
        double totalHours = WorkHoursCalculator.CalculateTotalHours(
            shiftStart: startTimeDecimal,
            shiftEnd: endTimeDecimal,
            breakDuration: totalBreak,
            manualAdjustment: manualAdjustment
        );

        // 4f) Decide how you combine partial vs single-day allowances:
        //     e.g., sum them or pick the larger. We'll just sum them here:

        // Build the PartRide
        var partRide = new PartRide
        {
            Id = Guid.NewGuid(),
            Date = segmentDate,
            Start = segmentStart,
            End = segmentEnd,
            Rest = request.Rest,
            Kilometers = request.Kilometers,
            CarId = TryParseGuid(request.CarId),
            DriverId = TryParseGuid(request.DriverId),
            Costs = request.Costs,
            Employer = request.Employer,
            ClientId = TryParseGuid(request.ClientId),
            Day = segmentDate.Day,
            WeekNumber = request.WeekNumber > 0 ? request.WeekNumber : GetIso8601WeekOfYear(segmentDate),
            UnitId = TryParseGuid(request.UnitId),
            RateId = TryParseGuid(request.RateId),
            CostsDescription = request.CostsDescription,
            SurchargeId = TryParseGuid(request.SurchargeId),
            Turnover = request.Turnover,
            Remark = request.Remark,
            CompanyId = companyGuid,
            CharterId = TryParseGuid(request.CharterId),
            RideId = TryParseGuid(request.RideId),

            // 5a) Fields that store the final hours:
            DecimalHours = totalHours, // old approach 
            CorrectionTotalHours = 0, // newly introduced field
            // 5b) Untaxed compensation fields
            TaxFreeCompensation = untaxedAllowanceSingleDay,
            NightAllowance = nightAllowance,
            // 5c) Not implementing logic for these fields yet, so default:
            StandOver = 0.0,
            KilometerReimbursement = 0.0,
            ExtraKilometers = 0.0,
            ConsignmentFee = 0.0,
            SaturdayHours = 0.0,
            SundayHolidayHours = 0.0,
            VariousCompensation = 0.0,
            // 5d) HoursOption & HoursCode - for now assume they're set from the request
            // HoursOptionId       = TryParseGuid(request.HoursOptionId),
            // HoursCodeId         = TryParseGuid(request.HoursCodeId)
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