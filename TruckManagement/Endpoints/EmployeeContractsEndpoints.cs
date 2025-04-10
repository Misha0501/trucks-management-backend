using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.Identity;
using TruckManagement.DTOs;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class EmployeeContractsEndpoints
    {
        public static void MapEmployeeContractsEndpoints(this WebApplication app)
        {
            app.MapPost("/employee-contracts",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    [FromBody] CreateEmployeeContractRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // 1) Basic checks
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not found or not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // 2) Validate optional DriverId
                        Guid? driverGuid = null;
                        if (!string.IsNullOrWhiteSpace(request.DriverId))
                        {
                            if (!Guid.TryParse(request.DriverId, out var parsedDriverId))
                            {
                                return ApiResponseFactory.Error("Invalid DriverId format.");
                            }

                            var driverExists = await db.Drivers.AnyAsync(d => d.Id == parsedDriverId);
                            if (!driverExists)
                            {
                                return ApiResponseFactory.Error("Specified driver not found.");
                            }

                            driverGuid = parsedDriverId;
                        }

                        // 3) Validate optional CompanyId
                        Guid? companyGuid = null;
                        if (!string.IsNullOrWhiteSpace(request.CompanyId))
                        {
                            if (!Guid.TryParse(request.CompanyId, out var parsedCompanyId))
                            {
                                return ApiResponseFactory.Error("Invalid CompanyId format.");
                            }

                            // check if the company actually exists
                            var companyEntity = await db.Companies
                                .FirstOrDefaultAsync(c => c.Id == parsedCompanyId);
                            if (companyEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "Specified company does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            // if not global admin, ensure user is contact-person for that company
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

                                // gather company Ids
                                var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                    .Select(cpc => cpc.CompanyId)
                                    .Distinct()
                                    .ToList();

                                if (!associatedCompanyIds.Contains(parsedCompanyId))
                                {
                                    return ApiResponseFactory.Error(
                                        "You are not authorized for the specified company.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }
                            }

                            companyGuid = parsedCompanyId;
                        }

                        // 4) Build the EmployeeContract entity
                        var contract = new EmployeeContract
                        {
                            Id = Guid.NewGuid(),
                            DriverId = driverGuid,
                            CompanyId = companyGuid,
                            ReleaseVersion = 1.0m,

                            NightHoursAllowed = request.NightHoursAllowed,
                            KilometersAllowanceAllowed = request.KilometersAllowanceAllowed,
                            CommuteKilometers = request.CommuteKilometers,

                            EmployeeFirstName = request.EmployeeFirstName,
                            EmployeeLastName = request.EmployeeLastName,
                            EmployeeAddress = request.EmployeeAddress,
                            EmployeePostcode = request.EmployeePostcode,
                            EmployeeCity = request.EmployeeCity,
                            DateOfBirth = request.DateOfBirth,
                            Bsn = request.Bsn,
                            DateOfEmployment = request.DateOfEmployment,
                            LastWorkingDay = request.LastWorkingDay,

                            Function = request.Function,
                            ProbationPeriod = request.ProbationPeriod,
                            WorkweekDuration = request.WorkweekDuration,
                            WeeklySchedule = request.WeeklySchedule,
                            WorkingHours = request.WorkingHours,
                            NoticePeriod = request.NoticePeriod,
                            CompensationPerMonthExclBtw = request.CompensationPerMonthExclBtw,
                            CompensationPerMonthInclBtw = request.CompensationPerMonthInclBtw,
                            PayScale = request.PayScale,
                            PayScaleStep = request.PayScaleStep,
                            HourlyWage100Percent = request.HourlyWage100Percent,
                            DeviatingWage = request.DeviatingWage,
                            TravelExpenses = request.TravelExpenses,
                            MaxTravelExpenses = request.MaxTravelExpenses,
                            VacationAge = request.VacationAge,
                            VacationDays = request.VacationDays,
                            Atv = request.Atv,
                            VacationAllowance = request.VacationAllowance,

                            CompanyName = request.CompanyName,
                            EmployerName = request.EmployerName,
                            CompanyAddress = request.CompanyAddress,
                            CompanyPostcode = request.CompanyPostcode,
                            CompanyCity = request.CompanyCity,
                            CompanyPhoneNumber = request.CompanyPhoneNumber,
                            CompanyBtw = request.CompanyBtw,
                            CompanyKvk = request.CompanyKvk
                        };

                        // 5) Save to DB
                        db.EmployeeContracts.Add(contract);
                        await db.SaveChangesAsync();

                        // 6) Return newly created data
                        var responseData = new
                        {
                            contract.Id,
                            contract.DriverId,
                            contract.CompanyId,
                            contract.ReleaseVersion,

                            contract.NightHoursAllowed,
                            contract.KilometersAllowanceAllowed,
                            contract.CommuteKilometers,

                            contract.EmployeeFirstName,
                            contract.EmployeeLastName,
                            contract.EmployeeAddress,
                            contract.EmployeePostcode,
                            contract.EmployeeCity,
                            contract.DateOfBirth,
                            contract.Bsn,
                            contract.DateOfEmployment,
                            contract.LastWorkingDay,

                            contract.Function,
                            contract.ProbationPeriod,
                            contract.WorkweekDuration,
                            contract.WeeklySchedule,
                            contract.WorkingHours,
                            contract.NoticePeriod,
                            contract.CompensationPerMonthExclBtw,
                            contract.CompensationPerMonthInclBtw,
                            contract.PayScale,
                            contract.PayScaleStep,
                            contract.HourlyWage100Percent,
                            contract.DeviatingWage,
                            contract.TravelExpenses,
                            contract.MaxTravelExpenses,
                            contract.VacationAge,
                            contract.VacationDays,
                            contract.Atv,
                            contract.VacationAllowance,

                            contract.CompanyName,
                            contract.EmployerName,
                            contract.CompanyAddress,
                            contract.CompanyPostcode,
                            contract.CompanyCity,
                            contract.CompanyPhoneNumber,
                            contract.CompanyBtw,
                            contract.CompanyKvk
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[EmployeeContract POST] Error: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while creating the EmployeeContract.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/employee-contracts",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, driver")]
                async (
                    string? companyId,
                    string? driverId,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db,
                    int pageNumber = 1,
                    int pageSize = 10
                ) =>
                {
                    try
                    {
                        // 1) Identify user
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        // 2) Roles
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isDriver = currentUser.IsInRole("driver");

                        // If driver, load driver entity
                        Guid? driverEntityId = null;
                        if (isDriver)
                        {
                            var driverEntity = await db.Drivers
                                .FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                            if (driverEntity == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No driver profile found or driver is deleted. You are not authorized.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            driverEntityId = driverEntity.Id;
                        }

                        // 3) Start an IQueryable
                        IQueryable<EmployeeContract> query = db.EmployeeContracts.AsQueryable();

                        // 4) If user is not globalAdmin and not driver => must be contact-person-based
                        //    If user is driver, we handle it later by forcing contract.DriverId==driver’s own Id
                        if (!isGlobalAdmin && !isDriver)
                        {
                            // gather contact-person data
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

                            // filter for only those contracts with CompanyId in associatedCompanyIds
                            query = query.Where(ec =>
                                ec.CompanyId.HasValue && associatedCompanyIds.Contains(ec.CompanyId.Value));
                        }

                        // 5) If user is a driver => only fetch where contract.DriverId == driverEntityId
                        if (isDriver)
                        {
                            query = query.Where(ec => ec.DriverId == driverEntityId);
                        }

                        // 6) If companyId is provided, parse & filter
                        if (!string.IsNullOrWhiteSpace(companyId))
                        {
                            if (!Guid.TryParse(companyId, out var parsedCompanyId))
                            {
                                return ApiResponseFactory.Error("Invalid companyId format.");
                            }

                            query = query.Where(ec => ec.CompanyId == parsedCompanyId);
                        }

                        // 7) If driverId is provided, parse & filter
                        if (!string.IsNullOrWhiteSpace(driverId))
                        {
                            if (!Guid.TryParse(driverId, out var parsedDriverId))
                            {
                                return ApiResponseFactory.Error("Invalid driverId format.");
                            }

                            query = query.Where(ec => ec.DriverId == parsedDriverId);
                        }

                        // 8) Pagination
                        if (pageNumber < 1) pageNumber = 1;
                        if (pageSize < 1) pageSize = 10;

                        var totalCount = await query.CountAsync();

                        var contracts = await query
                            .OrderBy(ec => ec.EmployeeLastName) // or any other order
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

                        // 9) Build response
                        var responseData = new
                        {
                            pageNumber,
                            pageSize,
                            totalCount,
                            data = contracts.Select(c => new
                            {
                                c.Id,
                                c.DriverId,
                                c.CompanyId,
                                c.ReleaseVersion,
                                c.NightHoursAllowed,
                                c.KilometersAllowanceAllowed,
                                c.CommuteKilometers,
                                c.EmployeeFirstName,
                                c.EmployeeLastName,
                                c.EmployeeAddress,
                                c.EmployeePostcode,
                                c.EmployeeCity,
                                c.DateOfBirth,
                                c.Bsn,
                                c.DateOfEmployment,
                                c.LastWorkingDay,
                                c.Function,
                                c.ProbationPeriod,
                                c.WorkweekDuration,
                                c.WeeklySchedule,
                                c.WorkingHours,
                                c.NoticePeriod,
                                c.CompensationPerMonthExclBtw,
                                c.CompensationPerMonthInclBtw,
                                c.PayScale,
                                c.PayScaleStep,
                                c.HourlyWage100Percent,
                                c.DeviatingWage,
                                c.TravelExpenses,
                                c.MaxTravelExpenses,
                                c.VacationAge,
                                c.VacationDays,
                                c.Atv,
                                c.VacationAllowance,
                                c.CompanyName,
                                c.EmployerName,
                                c.CompanyAddress,
                                c.CompanyPostcode,
                                c.CompanyCity,
                                c.CompanyPhoneNumber,
                                c.CompanyBtw,
                                c.CompanyKvk
                            })
                        };

                        return ApiResponseFactory.Success(responseData);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[EmployeeContracts GET] Error: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while fetching EmployeeContracts.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}