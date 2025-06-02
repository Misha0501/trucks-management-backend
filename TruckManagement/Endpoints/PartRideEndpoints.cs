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
using TruckManagement.Enums;
using TruckManagement.Helpers;
using TruckManagement.Services;
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
                    bool crossesMidnight = false;

                    // Not crossing if both are 00:00
                    if (startDecimal == 0 && endDecimal == 0)
                    {
                        crossesMidnight = false;
                    }
                    // Crossing if times are equal but not zero (e.g., 2:00 → 2:00)
                    else if (startDecimal == endDecimal)
                    {
                        crossesMidnight = true;
                    }
                    // Crossing if end is before start
                    else
                    {
                        crossesMidnight = endDecimal < startDecimal;
                    }

                    // 3) Validate references/roles once (driver, company, etc.)
                    var userId = userManager.GetUserId(currentUser)!;

                    var userRoles = (await userManager.GetRolesAsync(await userManager.GetUserAsync(currentUser)))
                        .ToList();

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
                            request.Date, request.Start, request.End,
                            userId,
                            userRoles,
                            request.HoursCorrection ?? 0
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
                            request.Date, request.Start, midnight,
                            userId,
                            userRoles,
                            request.HoursCorrection ?? 0
                        );
                        createdPartRides.Add(firstRide);

                        // Segment #2: from 00:00 -> End, next day
                        var secondRide = await CreateAndSavePartRideSegment(
                            db, request, companyGuid,
                            request.Date.AddDays(1),
                            TimeSpan.Zero,
                            request.End,
                            userId,
                            userRoles,
                            correctionHours: 0
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
                        pr.ClientId,
                        pr.DriverId,
                        pr.CompanyId,
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
                    Guid? newCharterId = TryParseGuid(request.CharterId);
                    Guid? newClientId = TryParseGuid(request.ClientId);
                    Guid? newHoursCodeId = TryParseGuid(request.HoursCodeId);
                    Guid? newHoursOptionId = TryParseGuid(request.HoursOptionId);

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

                    // Validate HoursCode
                    if (newHoursCodeId.HasValue)
                    {
                        var hoursCodeEntity = await db.HoursCodes.FindAsync(newHoursCodeId.Value);
                        if (hoursCodeEntity == null)
                        {
                            return ApiResponseFactory.Error(
                                "The specified hours code does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }
                    }

                    // Validate HoursOption if provided
                    if (newHoursOptionId.HasValue)
                    {
                        var hoursOptionEntity = await db.HoursOptions.FindAsync(newHoursOptionId.Value);
                        if (hoursOptionEntity == null)
                        {
                            return ApiResponseFactory.Error(
                                "The specified hours option does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }
                    }

                    // Update base fields if provided
                    if (request.Date.HasValue) existingPartRide.Date = request.Date.Value;
                    if (request.Start.HasValue) existingPartRide.Start = request.Start.Value;
                    if (request.End.HasValue) existingPartRide.End = request.End.Value;
                    existingPartRide.Kilometers = request.Kilometers;
                    existingPartRide.Costs = request.Costs;
                    existingPartRide.WeekNumber = request.WeekNumber;
                    existingPartRide.Turnover = request.Turnover;
                    existingPartRide.Remark = request.Remark;
                    existingPartRide.CostsDescription = request.CostsDescription;
                    existingPartRide.RideId = newRideId;
                    existingPartRide.CarId = newCarId;
                    existingPartRide.DriverId = newDriverId;
                    existingPartRide.CharterId = newCharterId;
                    existingPartRide.ClientId = newClientId;
                    existingPartRide.HoursCodeId = newHoursCodeId;
                    existingPartRide.HoursOptionId = newHoursOptionId;
                    existingPartRide.CorrectionTotalHours = request.HoursCorrection ?? 0;
                    existingPartRide.VariousCompensation = request.VariousCompensation ?? 0;

                    // If new company is set
                    if (currentCompanyId != Guid.Empty)
                    {
                        existingPartRide.CompanyId = currentCompanyId;
                    }

                    // 13) Check if updated End crosses midnight relative to Start
                    double startTimeDecimal = existingPartRide.Start.TotalHours;
                    double endTimeDecimal = existingPartRide.End.TotalHours;
                    bool crossesMidnight = false;
                    
                    // Not crossing if both are 00:00
                    if (startTimeDecimal == 0 && endTimeDecimal == 0)
                    {
                        crossesMidnight = false;
                    }
                    else if (startTimeDecimal == endTimeDecimal)
                    {
                        crossesMidnight = true;
                    }
                    else
                    {
                        crossesMidnight = endTimeDecimal < startTimeDecimal;
                    }

                    if (!crossesMidnight)
                    {
                        // 13a) If NOT crossing midnight, just recalc & save
                        await RecalculatePartRideValues(db, existingPartRide);
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
                        await RecalculatePartRideValues(db, existingPartRide);

                        // Create NEW PartRide for [00:00 -> new End], date is +1 day
                        var newPartRide = new PartRide
                        {
                            // copy relevant fields from existing
                            CompanyId = existingPartRide.CompanyId,
                            DriverId = existingPartRide.DriverId,
                            CarId = existingPartRide.CarId,
                            RideId = existingPartRide.RideId,
                            CharterId = existingPartRide.CharterId,
                            ClientId = existingPartRide.ClientId,

                            // date is next day
                            Date = existingPartRide.Date.AddDays(1),
                            Start = TimeSpan.FromHours(0),
                            End = TimeSpan.FromHours(endTimeDecimal),
                            Kilometers = existingPartRide.Kilometers,
                            Costs = existingPartRide.Costs,
                            WeekNumber = existingPartRide.WeekNumber,
                            Turnover = existingPartRide.Turnover,
                            Remark = existingPartRide.Remark,
                            CostsDescription = existingPartRide.CostsDescription,
                            HoursCodeId = existingPartRide.HoursCodeId,
                            HoursOptionId = existingPartRide.HoursOptionId,
                            VariousCompensation = 0,
                            CorrectionTotalHours = 0
                        };

                        // Recalculate new PartRide
                        await RecalculatePartRideValues(db, newPartRide);

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
                        .Include(pr => pr.Charter)
                        .Include(pr => pr.Ride)
                        .Include(pr => pr.HoursOption)
                        .Include(pr => pr.HoursCode)
                        .Include(pr => pr.Approvals)
                        .ThenInclude(a => a.Role)
                        .Include(pr => pr.Approvals)
                        .ThenInclude(a => a.ApprovedByUser)
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
                        Charter = partRide.Charter != null
                            ? new
                            {
                                partRide.Charter.Id,
                                partRide.Charter.Name
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
                        partRide.ConsignmentFee,
                        partRide.SaturdayHours,
                        partRide.SundayHolidayHours,
                        partRide.VariousCompensation,
                        partRide.NumberOfHours,
                        Approvals = partRide.Approvals.Select(a => new
                        {
                            a.Id,
                            a.Status,
                            a.UpdatedAt,
                            a.Comments,
                            Role = a.Role != null
                                ? new
                                {
                                    a.Role.Id,
                                    a.Role.Name
                                }
                                : null,
                            ApprovedByUser = a.ApprovedByUser != null
                                ? new
                                {
                                    a.ApprovedByUser.Id,
                                    a.ApprovedByUser.FirstName,
                                    a.ApprovedByUser.LastName,
                                    a.ApprovedByUser.Email
                                }
                                : null
                        }).OrderByDescending(a => a.UpdatedAt)
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

        app.MapPost("/partrides/{partRideId}/approve",
            [Authorize] async (
                string partRideId,
                [FromBody] ApprovePartRideRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (!Guid.TryParse(partRideId, out Guid parsedRideId))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid PartRide ID format.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    var partRide = await db.PartRides
                        .Include(pr => pr.Approvals)
                        .FirstOrDefaultAsync(pr => pr.Id == parsedRideId);

                    if (partRide == null)
                    {
                        return ApiResponseFactory.Error(
                            "PartRide not found.",
                            StatusCodes.Status404NotFound
                        );
                    }

                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    // Get user and roles
                    var creatingUser = await userManager.GetUserAsync(currentUser);
                    var userRoles = await userManager.GetRolesAsync(creatingUser);

                    bool isGlobalAdmin = userRoles.Contains("globalAdmin");
                    bool isDriver = userRoles.Contains("driver");
                    bool isCustomerAdmin = userRoles.Contains("customerAdmin");

                    // If not globalAdmin, check if user is in the company
                    if (!isGlobalAdmin)
                    {
                        if (!partRide.CompanyId.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "PartRide has no associated company. Approval not allowed.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        if (isDriver)
                        {
                            // Additional requirement: driver can only approve if they're the driver on this PartRide
                            var driverEntity = await db.Drivers
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                            if (driverEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not registered as a driver. Contact your administrator.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            if (!driverEntity.CompanyId.HasValue ||
                                driverEntity.CompanyId.Value != partRide.CompanyId.Value)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not part of this PartRide's company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Check that this driver is actually the assigned driver of the PartRide
                            if (!partRide.DriverId.HasValue || partRide.DriverId.Value != driverEntity.Id)
                            {
                                return ApiResponseFactory.Error(
                                    "Drivers can only approve PartRides where they are assigned as driver.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                        else
                        {
                            // Must be a contact person or other company-based user
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId && !cp.IsDeleted);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(c => c.CompanyId)
                                .Where(x => x.HasValue)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(partRide.CompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to approve a PartRide for this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                    }

                    // Confirm the user has either "driver" or "customerAdmin" or is "globalAdmin"
                    if (!isDriver && !isCustomerAdmin && !isGlobalAdmin)
                    {
                        return ApiResponseFactory.Error(
                            "Only driver, customerAdmin, or globalAdmin can approve this PartRide.",
                            StatusCodes.Status403Forbidden
                        );
                    }

                    // Decide which role name the user is "approving" under
                    // If they're globalAdmin, you might pick "globalAdmin" or treat them as a special case
                    string targetRoleName;
                    if (isDriver) targetRoleName = "driver";
                    else if (isCustomerAdmin) targetRoleName = "customerAdmin";
                    else targetRoleName = "globalAdmin"; // or you can skip a separate role

                    var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName);
                    if (roleEntity == null)
                    {
                        return ApiResponseFactory.Error(
                            $"Role '{targetRoleName}' not found.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Try to find an existing approval for that role
                    var existingApproval = partRide.Approvals
                        .OrderByDescending(a => a.UpdatedAt)
                        .FirstOrDefault(a => a.RoleId == roleEntity.Id);

                    if (existingApproval != null)
                    {
                        if (existingApproval.Status == ApprovalStatus.Pending)
                        {
                            // If it's Pending -> set it to Approved
                            existingApproval.Status = ApprovalStatus.Approved;
                            existingApproval.ApprovedByUserId = userId;
                            existingApproval.Comments = request.Comments;
                            existingApproval.UpdatedAt = DateTime.UtcNow;
                        }
                        else if (existingApproval.Status == ApprovalStatus.Rejected ||
                                 existingApproval.Status == ApprovalStatus.ChangesRequested)
                        {
                            // Instead of overriding, create a new approval in Approved state
                            var newApproval = new PartRideApproval
                            {
                                Id = Guid.NewGuid(),
                                PartRideId = partRide.Id,
                                RoleId = roleEntity.Id,
                                Status = ApprovalStatus.Approved,
                                ApprovedByUserId = userId,
                                Comments = request.Comments,
                                UpdatedAt = DateTime.UtcNow
                            };
                            db.PartRideApprovals.Add(newApproval);
                        }
                        else
                        {
                            // Already Approved or some other status we don't override
                            return ApiResponseFactory.Error(
                                $"Approval for role '{targetRoleName}' is already '{existingApproval.Status}'.",
                                StatusCodes.Status400BadRequest
                            );
                        }
                    }
                    else
                    {
                        // No existing approval for this role, create a new one in Approved state
                        var newApproval = new PartRideApproval
                        {
                            Id = Guid.NewGuid(),
                            PartRideId = partRide.Id,
                            RoleId = roleEntity.Id,
                            Status = ApprovalStatus.Approved,
                            ApprovedByUserId = userId,
                            Comments = request.Comments,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.PartRideApprovals.Add(newApproval);
                    }

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(
                        "Approval recorded successfully.",
                        StatusCodes.Status200OK
                    );
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error approving PartRide: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while approving the PartRide.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );

        app.MapPost("/partrides/{partRideId}/reject",
            [Authorize] async (
                string partRideId,
                [FromBody] RejectPartRideRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (!Guid.TryParse(partRideId, out Guid parsedRideId))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid PartRide ID format.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    var partRide = await db.PartRides
                        .Include(pr => pr.Approvals)
                        .FirstOrDefaultAsync(pr => pr.Id == parsedRideId);

                    if (partRide == null)
                    {
                        return ApiResponseFactory.Error(
                            "PartRide not found.",
                            StatusCodes.Status404NotFound
                        );
                    }

                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    var creatingUser = await userManager.GetUserAsync(currentUser);
                    var userRoles = await userManager.GetRolesAsync(creatingUser);

                    bool isGlobalAdmin = userRoles.Contains("globalAdmin");
                    bool isDriver = userRoles.Contains("driver");
                    bool isCustomerAdmin = userRoles.Contains("customerAdmin");

                    if (!isGlobalAdmin)
                    {
                        if (!partRide.CompanyId.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "PartRide has no associated company. Reject not allowed.",
                                StatusCodes.Status400BadRequest
                            );
                        }

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

                            if (!driverEntity.CompanyId.HasValue ||
                                driverEntity.CompanyId.Value != partRide.CompanyId.Value)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not part of this PartRide's company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Driver can only reject if they are assigned to this PartRide
                            if (!partRide.DriverId.HasValue || partRide.DriverId.Value != driverEntity.Id)
                            {
                                return ApiResponseFactory.Error(
                                    "Drivers can only reject PartRides where they are assigned as driver.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                        else
                        {
                            // Must be customerAdmin or other contact-person
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId && !cp.IsDeleted);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(c => c.CompanyId)
                                .Where(x => x.HasValue)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(partRide.CompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to reject a PartRide for this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                    }

                    // Make sure user is driver, customerAdmin, or globalAdmin
                    if (!isDriver && !isCustomerAdmin && !isGlobalAdmin)
                    {
                        return ApiResponseFactory.Error(
                            "Only driver, customerAdmin, or globalAdmin can reject this PartRide.",
                            StatusCodes.Status403Forbidden
                        );
                    }

                    // Determine the role name for the rejection
                    string targetRoleName;
                    if (isDriver) targetRoleName = "driver";
                    else if (isCustomerAdmin) targetRoleName = "customerAdmin";
                    else targetRoleName = "globalAdmin";

                    var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName);
                    if (roleEntity == null)
                    {
                        return ApiResponseFactory.Error(
                            $"Role '{targetRoleName}' not found.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Get the "latest" approval for this role
                    var existingApproval = partRide.Approvals
                        .Where(a => a.RoleId == roleEntity.Id)
                        .OrderByDescending(a => a.UpdatedAt)
                        .FirstOrDefault();

                    if (existingApproval != null)
                    {
                        // If Pending or ChangesRequested -> set to Rejected
                        if (existingApproval.Status == ApprovalStatus.Pending)
                        {
                            existingApproval.Status = ApprovalStatus.Rejected;
                            existingApproval.ApprovedByUserId = userId;
                            existingApproval.Comments = request.Comments;
                            existingApproval.UpdatedAt = DateTime.UtcNow;
                        }
                        else if (existingApproval.Status == ApprovalStatus.Rejected)
                        {
                            // Already Rejected => error or create a new record
                            return ApiResponseFactory.Error(
                                $"Approval for role '{targetRoleName}' is already Rejected.",
                                StatusCodes.Status400BadRequest
                            );
                        }
                        else
                        {
                            // It's Approved or some other status
                            // If you prefer to let them create a new record in Rejected state, do so here:
                            var newApproval = new PartRideApproval
                            {
                                Id = Guid.NewGuid(),
                                PartRideId = partRide.Id,
                                RoleId = roleEntity.Id,
                                Status = ApprovalStatus.Rejected,
                                ApprovedByUserId = userId,
                                Comments = request.Comments,
                                UpdatedAt = DateTime.UtcNow
                            };
                            db.PartRideApprovals.Add(newApproval);
                        }
                    }
                    else
                    {
                        // No existing record for that role => create new in Rejected
                        var newApproval = new PartRideApproval
                        {
                            Id = Guid.NewGuid(),
                            PartRideId = partRide.Id,
                            RoleId = roleEntity.Id,
                            Status = ApprovalStatus.Rejected,
                            ApprovedByUserId = userId,
                            Comments = request.Comments,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.PartRideApprovals.Add(newApproval);
                    }

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(
                        "PartRide was rejected successfully.",
                        StatusCodes.Status200OK
                    );
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error rejecting PartRide: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while rejecting the PartRide.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );

        app.MapPost("/partrides/{partRideId}/changes-requested",
            [Authorize] async (
                string partRideId,
                [FromBody] ChangesRequestedPartRideRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (!Guid.TryParse(partRideId, out Guid parsedRideId))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid PartRide ID format.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    var partRide = await db.PartRides
                        .Include(pr => pr.Approvals)
                        .FirstOrDefaultAsync(pr => pr.Id == parsedRideId);

                    if (partRide == null)
                    {
                        return ApiResponseFactory.Error(
                            "PartRide not found.",
                            StatusCodes.Status404NotFound
                        );
                    }

                    var userId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    var creatingUser = await userManager.GetUserAsync(currentUser);
                    var userRoles = await userManager.GetRolesAsync(creatingUser);

                    bool isGlobalAdmin = userRoles.Contains("globalAdmin");
                    bool isDriver = userRoles.Contains("driver");
                    bool isCustomerAdmin = userRoles.Contains("customerAdmin");

                    if (!isGlobalAdmin)
                    {
                        if (!partRide.CompanyId.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "PartRide has no associated company. Changes request not allowed.",
                                StatusCodes.Status400BadRequest
                            );
                        }

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

                            if (!driverEntity.CompanyId.HasValue ||
                                driverEntity.CompanyId.Value != partRide.CompanyId.Value)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not part of this PartRide's company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Driver can only do changes-requested if they're the assigned driver
                            if (!partRide.DriverId.HasValue || partRide.DriverId.Value != driverEntity.Id)
                            {
                                return ApiResponseFactory.Error(
                                    "Drivers can only request changes to PartRides where they are assigned.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                        else
                        {
                            // Must be contact-person or other roles in the same company
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId && !cp.IsDeleted);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(c => c.CompanyId)
                                .Where(x => x.HasValue)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(partRide.CompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to request changes for this PartRide.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }
                    }

                    // Must be driver, customerAdmin, or globalAdmin
                    if (!isDriver && !isCustomerAdmin && !isGlobalAdmin)
                    {
                        return ApiResponseFactory.Error(
                            "Only driver, customerAdmin, or globalAdmin can request changes.",
                            StatusCodes.Status403Forbidden
                        );
                    }

                    // Decide which role name
                    string targetRoleName;
                    if (isDriver) targetRoleName = "driver";
                    else if (isCustomerAdmin) targetRoleName = "customerAdmin";
                    else targetRoleName = "globalAdmin"; // or some special logic

                    var roleEntity = await db.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName);
                    if (roleEntity == null)
                    {
                        return ApiResponseFactory.Error(
                            $"Role '{targetRoleName}' not found.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // Retrieve the latest existing approval for this role
                    var existingApproval = partRide.Approvals
                        .Where(a => a.RoleId == roleEntity.Id)
                        .OrderByDescending(a => a.UpdatedAt)
                        .FirstOrDefault();

                    if (existingApproval != null)
                    {
                        // Example logic: if it's Approved, Rejected, or Pending, create new changes requested
                        // You can adjust to only do it if Approved or something else
                        switch (existingApproval.Status)
                        {
                            case ApprovalStatus.Pending:
                            case ApprovalStatus.Approved:
                            case ApprovalStatus.Rejected:
                                // Create a new record in ChangesRequested
                                var newApproval = new PartRideApproval
                                {
                                    Id = Guid.NewGuid(),
                                    PartRideId = partRide.Id,
                                    RoleId = roleEntity.Id,
                                    Status = ApprovalStatus.ChangesRequested,
                                    ApprovedByUserId = userId,
                                    Comments = request.Comments,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                db.PartRideApprovals.Add(newApproval);
                                break;

                            case ApprovalStatus.ChangesRequested:
                                return ApiResponseFactory.Error(
                                    $"There is already a 'ChangesRequested' record for role '{targetRoleName}'.",
                                    StatusCodes.Status400BadRequest
                                );

                            default:
                                return ApiResponseFactory.Error(
                                    $"Cannot request changes when current status is '{existingApproval.Status}'.",
                                    StatusCodes.Status400BadRequest
                                );
                        }
                    }
                    else
                    {
                        // No existing record => create new in ChangesRequested
                        var newApproval = new PartRideApproval
                        {
                            Id = Guid.NewGuid(),
                            PartRideId = partRide.Id,
                            RoleId = roleEntity.Id,
                            Status = ApprovalStatus.ChangesRequested,
                            ApprovedByUserId = userId,
                            Comments = request.Comments,
                            UpdatedAt = DateTime.UtcNow
                        };
                        db.PartRideApprovals.Add(newApproval);
                    }

                    await db.SaveChangesAsync();

                    return ApiResponseFactory.Success(
                        "Changes Requested recorded successfully.",
                        StatusCodes.Status200OK
                    );
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error requesting changes on PartRide: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while requesting changes on the PartRide.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );
    }

    private static Guid? TryParseGuid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        return Guid.TryParse(input, out var parsed) ? parsed : null;
    }

    // Example of an ISO8601 week calculation

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

    private static async Task RecalculatePartRideValues(
        ApplicationDbContext db,
        PartRide partRide
    )
    {
        // Make sure there's a valid DriverId
        if (!partRide.DriverId.HasValue)
        {
            throw new InvalidOperationException("Cannot recalculate compensation: PartRide has no DriverId.");
        }

        // Load the compensation settings for the driver
        var compensation = await db.DriverCompensationSettings
            .FirstOrDefaultAsync(dcs => dcs.DriverId == partRide.DriverId.Value);

        if (compensation == null)
        {
            throw new InvalidOperationException("No DriverCompensationSettings found for this driver.");
        }

        // Load HoursCode with default fallback
        Guid defaultHoursCodeId = Guid.Parse("AAAA1111-1111-1111-1111-111111111111");
        var hoursCodeId = partRide.HoursCodeId ?? defaultHoursCodeId;
        var hoursCode = await db.HoursCodes.FindAsync(hoursCodeId);
        if (hoursCode == null)
        {
            throw new InvalidOperationException($"HoursCode not found for ID: {hoursCodeId}");
        }

        // Load HoursOption if exists
        HoursOption? hoursOption = null;
        if (partRide.HoursOptionId.HasValue)
        {
            hoursOption = await db.HoursOptions.FindAsync(partRide.HoursOptionId.Value);
        }
        
        var caoService = new CaoService(db);
        var caoRow = caoService.GetCaoRow(partRide.Date);
        if (caoRow == null)
            throw new InvalidOperationException("No CAO entry for this date.");

        // 3) Create the calculator
        var workHoursCalculator = new WorkHoursCalculator(caoRow);

        var kilometersAllowanceCalculator = new KilometersAllowance(caoRow);

        var nightAllowanceCalculator = new NightAllowanceCalculator(caoRow); // pass the CAO row

        // Recompute time calculations based on updated Start/End/Rest
        double startTimeDecimal = partRide.Start.TotalHours;
        double endTimeDecimal = partRide.End.TotalHours;

        // Calculate additional allowances and totals using helper functions
        double untaxedAllowanceNormalDayPartial =
            workHoursCalculator.CalculateUntaxedAllowanceNormalDayPartial(
                startOfShift: startTimeDecimal,
                endOfShift: endTimeDecimal,
                isHoliday: false
            );

        double untaxedAllowanceSingleDay = workHoursCalculator.CalculateUntaxedAllowanceSingleDay(
            hourCode: hoursCode.Name,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            untaxedAllowanceNormalDayPartial: untaxedAllowanceNormalDayPartial
        );

        string holidayName = workHoursCalculator.GetHolidayName(
            date: partRide.Date,
            hoursOptionName: hoursOption?.Name
        );

        double calculatedSickHours = workHoursCalculator.CalculateSickHours(
            hourCode: hoursCode.Name,
            holidayName: holidayName,
            weeklyPercentage: compensation.PercentageOfWork,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double vacationHours = workHoursCalculator.CalculateVacationHours(
            hourCode: hoursCode.Name,
            weeklyPercentage: compensation.PercentageOfWork,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double calculatedNightAllowance = nightAllowanceCalculator.CalculateNightAllowance(
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            nightHoursAllowed: compensation.NightHoursAllowed,
            driverRate: (double)compensation.DriverRatePerHour,
            nightHoursWholeHours: false
        );

        double totalBreak = workHoursCalculator.CalculateTotalBreak(
            breakScheduleOn: true,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            hourCode: hoursCode.Name,
            sickHours: calculatedSickHours,
            vacationHours: vacationHours
        );

        double homeWorkDistance = kilometersAllowanceCalculator.HomeWorkDistance(
            kilometerAllowanceEnabled: compensation.KilometerAllowanceEnabled,
            oneWayValue: (double)compensation.KilometersOneWayValue
        );
        TimeSpan restTimeSpan = TimeSpan.FromHours(totalBreak);

        double totalHours = workHoursCalculator.CalculateTotalHours(
            shiftStart: startTimeDecimal,
            shiftEnd: endTimeDecimal,
            breakDuration: totalBreak,
            manualAdjustment: partRide.CorrectionTotalHours
        );

        double untaxedAllowanceDepartureDay = workHoursCalculator.CalculateUntaxedAllowanceDepartureDay(
            hourCode: hoursCode.Name,
            departureStartTime: startTimeDecimal
        );

        double untaxedAllowanceIntermediateDay = workHoursCalculator.CalculateUntaxedAllowanceIntermediateDay(
            hourCode: hoursCode.Name,
            hourOption: hoursOption?.Name,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double untaxedAllowanceArrivalDay = workHoursCalculator.CalculateUntaxedAllowanceArrivalDay(
            hourCode: hoursCode.Name,
            arrivalEndTime: endTimeDecimal
        );

        double taxFreeCompensation = untaxedAllowanceSingleDay + untaxedAllowanceDepartureDay +
                                     untaxedAllowanceIntermediateDay + untaxedAllowanceArrivalDay;

        double consignmentAllowance = workHoursCalculator.CalculateConsignmentAllowance(
            hourCode: hoursCode.Name,
            dateLookup: partRide.Date,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double saturdayHours = workHoursCalculator.CalculateSaturdayHours(
            date: partRide.Date,
            holidayName: holidayName,
            hoursCode: hoursCode.Name,
            totalHours: totalHours
        );

        double sundayHolidayHours = workHoursCalculator.CalculateSundayHolidayHours(
            date: partRide.Date,
            holidayName: holidayName,
            hourCode: hoursCode.Name,
            totalHours: totalHours
        );

        double netHours = workHoursCalculator.CalculateNetHours(
            hourCode: hoursCode.Name,
            day: partRide.Date,
            isHoliday: !string.IsNullOrWhiteSpace(holidayName),
            totalHours: totalHours,
            weeklyPercentage: compensation.PercentageOfWork
        );
        
        double kilometersAllowance = kilometersAllowanceCalculator.CalculateKilometersAllowance(
            extraKilometers: partRide.Kilometers ?? 0,
            hourCode: hoursCode.Name,
            hourOption: hoursOption?.Name,
            totalHours: netHours,
            homeWorkDistance: homeWorkDistance
        );
        
        var (year, periodNr, weekNrInPeriod) = DateHelper.GetPeriod(partRide.Date);

        partRide.Rest = restTimeSpan;
        partRide.DecimalHours = netHours;
        partRide.TaxFreeCompensation = taxFreeCompensation;
        partRide.NightAllowance = calculatedNightAllowance;
        partRide.StandOver = 0.0;
        partRide.KilometerReimbursement = kilometersAllowance;
        partRide.ConsignmentFee = consignmentAllowance;
        partRide.SaturdayHours = saturdayHours;
        partRide.SundayHolidayHours = sundayHolidayHours;
        partRide.NumberOfHours = totalHours;
        partRide.PeriodNumber = periodNr;
        partRide.WeekNrInPeriod = weekNrInPeriod;
        
        var periodApproval = await PeriodApprovalService.GetOrCreateAsync(db, partRide.DriverId.Value, partRide.Date);
        partRide.PeriodApprovalId = periodApproval.Id;
        
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
            pr.ClientId,
            pr.CompanyId,
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
        Guid? charterGuid = TryParseGuid(request.CharterId);
        Guid? clientGuid = TryParseGuid(request.ClientId);

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
        TimeSpan segmentEnd,
        string creatingUserId,
        List<string> creatingUserRoles,
        double correctionHours
    )
    {
        Guid defaultHoursCodeId = Guid.Parse("AAAA1111-1111-1111-1111-111111111111"); // "One day ride"

        // Get HoursCode - use default if not specified
        var hoursCodeId = TryParseGuid(request.HoursCodeId) ?? defaultHoursCodeId;
        var hoursCode = await db.HoursCodes.FindAsync(hoursCodeId);
        if (hoursCode == null)
        {
            throw new InvalidOperationException($"HoursCode not found for ID: {hoursCodeId}");
        }

        // Get HoursOption - can be null
        HoursOption? hoursOption = null;
        if (TryParseGuid(request.HoursOptionId) is Guid hoursOptionId)
        {
            hoursOption = await db.HoursOptions.FindAsync(hoursOptionId);
            if (hoursOption == null)
            {
                throw new InvalidOperationException($"HoursOption not found for ID: {hoursOptionId}");
            }
        }

        // 1) Convert Start/End to decimal hours
        double startTimeDecimal = segmentStart.TotalHours;
        double endTimeDecimal = segmentEnd.TotalHours;

        // If you allow crossing midnight in these segments, rawTime could be negative. 
        // Typically you'd handle that outside or split into two segments.

        // 3) Some basic calculations from your request:
        //    (If you prefer reading from request directly, feel free.)

        // Fetch DriverCompensationSettings
        var driverId = TryParseGuid(request.DriverId);
        var compensation = await db.DriverCompensationSettings
            .FirstOrDefaultAsync(c => c.DriverId == driverId);

        if (compensation == null)
        {
            throw new InvalidOperationException("DriverCompensationSettings not found for the specified driver.");
        }
        
        var caoService = new CaoService(db);
        var caoRow = caoService.GetCaoRow(request.Date);
        if (caoRow == null)
            throw new InvalidOperationException("No CAO entry for this date.");

        // 3) Create the calculator
        var workHoursCalculator = new WorkHoursCalculator(caoRow);
        var kilometersAllowanceCalculator = new KilometersAllowance(caoRow);
        var nightAllowanceCalculator = new NightAllowanceCalculator(caoRow); // pass the CAO row

        // 4) RUN THE CALCULATIONS:
        string holidayName = workHoursCalculator.GetHolidayName(
            date: request.Date,
            hoursOptionName: hoursOption?.Name
        );
        
        // 4a) Untaxed allowances
        double untaxedAllowanceNormalDayPartial = workHoursCalculator.CalculateUntaxedAllowanceNormalDayPartial(
            startOfShift: startTimeDecimal,
            endOfShift: endTimeDecimal,
            isHoliday: !string.IsNullOrWhiteSpace(holidayName)
        );

        double untaxedAllowanceSingleDay = workHoursCalculator.CalculateUntaxedAllowanceSingleDay(
            hourCode: hoursCode.Name,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            untaxedAllowanceNormalDayPartial: untaxedAllowanceNormalDayPartial
        );

        // 4b) Sick/Holiday hours
        double sickHours = workHoursCalculator.CalculateSickHours(
            hourCode: hoursCode.Name, // Replace with real hour code from request
            holidayName: holidayName, // Or request.HolidayName if you have it
            weeklyPercentage: compensation.PercentageOfWork,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );
        double vacationHours = workHoursCalculator.CalculateVacationHours(
            hourCode: hoursCode.Name,
            weeklyPercentage: compensation.PercentageOfWork,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        // 4c) Night allowance
        double nightAllowance = nightAllowanceCalculator.CalculateNightAllowance(
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            nightHoursAllowed: compensation.NightHoursAllowed,
            driverRate: (double)compensation.DriverRatePerHour,
            nightHoursWholeHours: false
        );

        // 4d) Break calculation (TotalBreak)
        double totalBreak = workHoursCalculator.CalculateTotalBreak(
            breakScheduleOn: true, // Always true
            startTime: startTimeDecimal,
            endTime: endTimeDecimal,
            hourCode: hoursCode.Name,
            sickHours: sickHours,
            vacationHours: vacationHours
        );
        TimeSpan restTimeSpan = TimeSpan.FromHours(totalBreak);

        // 4e) Compute final “totalHours” after subtracting the break, plus any manual adjustment
        double manualAdjustment = request.HoursCorrection ?? 0; // or request.ManualAdjustment if you have that
        double totalHours = workHoursCalculator.CalculateTotalHours(
            shiftStart: startTimeDecimal,
            shiftEnd: endTimeDecimal,
            breakDuration: totalBreak,
            manualAdjustment: manualAdjustment
        );

        double homeWorkDistance = kilometersAllowanceCalculator.HomeWorkDistance(
            kilometerAllowanceEnabled: compensation.KilometerAllowanceEnabled,
            oneWayValue: compensation.KilometersOneWayValue
        );

        double untaxedAllowanceDepartureDay = workHoursCalculator.CalculateUntaxedAllowanceDepartureDay(
            hourCode: hoursCode.Name,
            departureStartTime: startTimeDecimal
        );

        double untaxedAllowanceIntermediateDay = workHoursCalculator.CalculateUntaxedAllowanceIntermediateDay(
            hourCode: hoursCode.Name,
            hourOption: hoursOption?.Name,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double untaxedAllowanceArrivalDay = workHoursCalculator.CalculateUntaxedAllowanceArrivalDay(
            hourCode: hoursCode.Name,
            arrivalEndTime: endTimeDecimal
        );

        double taxFreeCompensation = untaxedAllowanceSingleDay + untaxedAllowanceDepartureDay +
                                     untaxedAllowanceIntermediateDay + untaxedAllowanceArrivalDay;

        double consignmentAllowance = workHoursCalculator.CalculateConsignmentAllowance(
            hourCode: hoursCode.Name,
            dateLookup: request.Date,
            startTime: startTimeDecimal,
            endTime: endTimeDecimal
        );

        double saturdayHours = workHoursCalculator.CalculateSaturdayHours(
            date: request.Date,
            holidayName: holidayName,
            hoursCode: hoursCode.Name,
            totalHours: totalHours
        );

        double sundayHolidayHours = workHoursCalculator.CalculateSundayHolidayHours(
            date: request.Date,
            holidayName: holidayName,
            hourCode: hoursCode.Name,
            totalHours: totalHours
        );

        double netHours = workHoursCalculator.CalculateNetHours(
            hourCode: hoursCode.Name,
            day: request.Date,
            isHoliday: !string.IsNullOrWhiteSpace(holidayName),
            totalHours: totalHours,
            weeklyPercentage: compensation.PercentageOfWork
        );
        
        double kilometersAllowance = kilometersAllowanceCalculator.CalculateKilometersAllowance(
            extraKilometers: request.Kilometers,
            hourCode: hoursCode.Name,
            hourOption: hoursOption?.Name,
            totalHours: netHours,
            homeWorkDistance: homeWorkDistance
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
            Rest = restTimeSpan,
            Kilometers = request.Kilometers,
            CarId = TryParseGuid(request.CarId),
            DriverId = TryParseGuid(request.DriverId),
            Costs = request.Costs,
            ClientId = TryParseGuid(request.ClientId),
            WeekNumber = request.WeekNumber > 0 ? request.WeekNumber : DateHelper.GetIso8601WeekOfYear(segmentDate),
            CostsDescription = request.CostsDescription,
            Turnover = request.Turnover,
            Remark = request.Remark,
            CompanyId = companyGuid,
            CharterId = TryParseGuid(request.CharterId),
            RideId = TryParseGuid(request.RideId),

            DecimalHours = netHours,
            CorrectionTotalHours = correctionHours,
            TaxFreeCompensation = taxFreeCompensation,
            NightAllowance = nightAllowance,
            StandOver = 0.0,
            KilometerReimbursement = kilometersAllowance,
            ConsignmentFee = consignmentAllowance,
            SaturdayHours = saturdayHours,
            SundayHolidayHours = sundayHolidayHours,
            NumberOfHours = totalHours,
            VariousCompensation = request.VariousCompensation ?? 0,
            HoursOptionId = TryParseGuid(request.HoursOptionId),
            HoursCodeId = TryParseGuid(request.HoursCodeId),
        };

        if (driverId.HasValue)
        {
            var periodApproval = await PeriodApprovalService.GetOrCreateAsync(db, driverId.Value, segmentDate);
            partRide.PeriodApprovalId = periodApproval.Id;
        }
        var (year, periodNr, weekNrInPeriod) = DateHelper.GetPeriod(partRide.Date);
        partRide.PeriodNumber = periodNr;
        partRide.WeekNrInPeriod = weekNrInPeriod;
        
        // Save
        db.PartRides.Add(partRide);
        await db.SaveChangesAsync();

        // Approval creation 
        var driverRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "driver");
        var contactRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "customerAdmin");

        // Default: both approvals pending
        var driverApprovalStatus = ApprovalStatus.Pending;
        var contactApprovalStatus = ApprovalStatus.Pending;

        string? driverApprover = null;
        string? contactApprover = null;

        // Logic based on creator's role
        if (creatingUserRoles.Contains("driver"))
        {
            driverApprovalStatus = ApprovalStatus.Approved;
            driverApprover = creatingUserId;
        }
        else if (creatingUserRoles.Contains("customerAdmin") || creatingUserRoles.Contains("customer"))
        {
            contactApprovalStatus = ApprovalStatus.Approved;
            contactApprover = creatingUserId;
        }
        // Else: globalAdmin, employer, etc. => both remain pending

        // Create driver approval
        var driverApproval = new PartRideApproval
        {
            Id = Guid.NewGuid(),
            PartRideId = partRide.Id,
            RoleId = driverRole?.Id ?? "",
            Status = driverApprovalStatus,
            ApprovedByUserId = driverApprovalStatus == ApprovalStatus.Approved
                ? driverApprover
                : null,
            UpdatedAt = DateTime.UtcNow
        };

        // Create contact-person approval
        var contactApproval = new PartRideApproval
        {
            Id = Guid.NewGuid(),
            PartRideId = partRide.Id,
            RoleId = contactRole?.Id ?? "",
            Status = contactApprovalStatus,
            ApprovedByUserId = contactApprovalStatus == ApprovalStatus.Approved
                ? contactApprover
                : null,
            UpdatedAt = DateTime.UtcNow
        };

        // Attach to PartRide and save
        db.PartRideApprovals.Add(driverApproval);
        db.PartRideApprovals.Add(contactApproval);
        await db.SaveChangesAsync();

        return partRide;
    }
}