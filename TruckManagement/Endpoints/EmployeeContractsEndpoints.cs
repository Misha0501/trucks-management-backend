using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.Identity;
using TruckManagement.DTOs;
using TruckManagement.Enums;
using TruckManagement.Helpers;
using TruckManagement.Services;

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

                        if (string.IsNullOrWhiteSpace(request.CompanyId))
                        {
                            return ApiResponseFactory.Error("CompanyId is required.", StatusCodes.Status400BadRequest);
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
                            CompanyKvk = request.CompanyKvk,
                            AccessCode = ContractAccessCodeGenerator.Generate(),
                            Status = EmployeeContractStatus.Pending
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
                        IQueryable<EmployeeContract> query = db.EmployeeContracts
                            .Include(c => c.Driver)
                            .ThenInclude(d => d.User)
                            .Include(c => c.Company)
                            .AsQueryable();

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
                                Driver = c.Driver == null
                                    ? null
                                    : new
                                    {
                                        c.Driver.Id,
                                        FullName = c.Driver.User.FirstName + " " + c.Driver.User.LastName,
                                        c.Driver.AspNetUserId
                                    },
                                Company = c.Company == null
                                    ? null
                                    : new
                                    {
                                        c.Company.Id,
                                        c.Company.Name
                                    },
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
                                c.CompanyKvk,
                                c.Status
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

            app.MapGet("/employee-contracts/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, driver")]
                async (
                    Guid id,
                    ApplicationDbContext db,
                    ClaimsPrincipal currentUser,
                    UserManager<ApplicationUser> userManager
                ) =>
                {
                    try
                    {
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated",
                                StatusCodes.Status401Unauthorized);
                        }

                        var contract = await db.EmployeeContracts
                            .Include(ec => ec.Driver)
                            .ThenInclude(d => d.User)
                            .Include(ec => ec.Company)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ec => ec.Id == id);

                        if (contract == null)
                        {
                            return ApiResponseFactory.Error("EmployeeContract not found",
                                StatusCodes.Status404NotFound);
                        }

                        // Check access for driver
                        if (currentUser.IsInRole("driver"))
                        {
                            var driver = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId);
                            if (driver == null || contract.DriverId != driver.Id)
                            {
                                return ApiResponseFactory.Error("Access denied: Not your contract.",
                                    StatusCodes.Status403Forbidden);
                            }
                        }
                        else if (!currentUser.IsInRole("globalAdmin"))
                        {
                            // Validate user belongs to company (admin side)
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Access denied: No contact person profile found.",
                                    StatusCodes.Status403Forbidden);
                            }

                            var userCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cc => cc.CompanyId)
                                .ToList();

                            if (contract.CompanyId == null || !userCompanyIds.Contains(contract.CompanyId))
                            {
                                return ApiResponseFactory.Error("Access denied: Not part of the specified company.",
                                    StatusCodes.Status403Forbidden);
                            }
                        }

                        // Shape response with Driver & Company info
                        var contractResponse = new
                        {
                            contract.Id,
                            Driver = contract.Driver == null
                                ? null
                                : new
                                {
                                    contract.Driver.Id,
                                    FullName = contract.Driver.User.FirstName + " " + contract.Driver.User.LastName,
                                    contract.Driver.AspNetUserId
                                },
                            Company = contract.Company == null
                                ? null
                                : new
                                {
                                    contract.Company.Id,
                                    contract.Company.Name
                                },
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
                            contract.CompanyKvk,
                            contract.Status,
                            contract.SignedAt,
                            contract.AccessCode
                        };


                        return ApiResponseFactory.Success(contractResponse);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching contract detail: {ex.Message}");
                        return ApiResponseFactory.Error("Unexpected error while retrieving the contract.",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            app.MapDelete("/employee-contracts/{id:guid}",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        var isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        var contract = await db.EmployeeContracts.FindAsync(id);

                        if (contract == null)
                        {
                            return ApiResponseFactory.Error("Employee contract not found.",
                                StatusCodes.Status404NotFound);
                        }

                        if (!isGlobalAdmin && contract.CompanyId != null)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("You are not authorized to delete this contract.",
                                    StatusCodes.Status403Forbidden);
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            if (!associatedCompanyIds.Contains(contract.CompanyId.Value))
                            {
                                return ApiResponseFactory.Error("You are not authorized to delete this contract.",
                                    StatusCodes.Status403Forbidden);
                            }
                        }

                        db.EmployeeContracts.Remove(contract);
                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success(new { Message = "Employee contract deleted successfully." },
                            StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error deleting employee contract: {ex.Message}");
                        return ApiResponseFactory.Error("Internal server error while deleting the employee contract.",
                            StatusCodes.Status500InternalServerError);
                    }
                }
            );

            app.MapPut("/employee-contracts/{id:guid}",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
                async (
                    Guid id,
                    [FromBody] UpdateEmployeeContractRequest request,
                    ClaimsPrincipal currentUser,
                    UserManager<ApplicationUser> userManager,
                    ApplicationDbContext db
                ) =>
                {
                    try
                    {
                        // 1) Basic user check
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // 2) Load existing contract
                        var contract = await db.EmployeeContracts.FirstOrDefaultAsync(ec => ec.Id == id);
                        if (contract == null)
                        {
                            return ApiResponseFactory.Error("Employee contract not found.",
                                StatusCodes.Status404NotFound);
                        }

                        // 3) Validate optional CompanyId
                        Guid? finalCompanyId = null;

                        if (string.IsNullOrWhiteSpace(request.CompanyId))
                        {
                            return ApiResponseFactory.Error("CompanyId is required.", StatusCodes.Status400BadRequest);
                        }

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

                            finalCompanyId = parsedCompanyId;
                        }

                        // 4) Validate optional DriverId
                        Guid? finalDriverId = null;
                        if (!string.IsNullOrWhiteSpace(request.DriverId))
                        {
                            if (!Guid.TryParse(request.DriverId, out var parsedDriverId))
                            {
                                return ApiResponseFactory.Error("Invalid DriverId format.");
                            }

                            // check if driver exists
                            var driver = await db.Drivers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.Id == parsedDriverId);

                            if (driver == null)
                            {
                                return ApiResponseFactory.Error("The specified driver does not exist or is deleted.");
                            }

                            if (driver.CompanyId == null || driver.CompanyId != finalCompanyId)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified driver does not belong to the selected company.");
                            }

                            finalDriverId = parsedDriverId;
                        }

                        // 5) If not global admin, ensure the user is contact-person for finalCompanyId
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden);
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            // ❗ Ensure contract being updated belongs to one of the user's companies
                            if (contract.CompanyId.HasValue && !associatedCompanyIds.Contains(contract.CompanyId.Value))
                            {
                                return ApiResponseFactory.Error("You are not authorized to modify this contract.",
                                    StatusCodes.Status403Forbidden);
                            }

                            // If CompanyId is being changed, make sure it's to a company the contact person is part of
                            if (!associatedCompanyIds.Contains(finalCompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to assign this contract to the specified company.",
                                    StatusCodes.Status403Forbidden);
                            }
                        }

                        // 6) Update the contract
                        contract.CompanyId = finalCompanyId;
                        contract.DriverId = finalDriverId;

                        contract.NightHoursAllowed = request.NightHoursAllowed;
                        contract.KilometersAllowanceAllowed = request.KilometersAllowanceAllowed;
                        contract.CommuteKilometers = request.CommuteKilometers;

                        contract.EmployeeFirstName = request.EmployeeFirstName;
                        contract.EmployeeLastName = request.EmployeeLastName;
                        contract.EmployeeAddress = request.EmployeeAddress;
                        contract.EmployeePostcode = request.EmployeePostcode;
                        contract.EmployeeCity = request.EmployeeCity;
                        contract.DateOfBirth = request.DateOfBirth;
                        contract.Bsn = request.Bsn;
                        contract.DateOfEmployment = request.DateOfEmployment;
                        contract.LastWorkingDay = request.LastWorkingDay;

                        contract.Function = request.Function;
                        contract.ProbationPeriod = request.ProbationPeriod;
                        contract.WorkweekDuration = request.WorkweekDuration;
                        contract.WorkweekDurationPercentage = request.WorkweekDurationPercentage;
                        contract.WeeklySchedule = request.WeeklySchedule;
                        contract.WorkingHours = request.WorkingHours;
                        contract.NoticePeriod = request.NoticePeriod;
                        contract.CompensationPerMonthExclBtw = request.CompensationPerMonthExclBtw;
                        contract.CompensationPerMonthInclBtw = request.CompensationPerMonthInclBtw;
                        contract.PayScale = request.PayScale;
                        contract.PayScaleStep = request.PayScaleStep;
                        contract.HourlyWage100Percent = request.HourlyWage100Percent;
                        contract.DeviatingWage = request.DeviatingWage;
                        contract.TravelExpenses = request.TravelExpenses;
                        contract.MaxTravelExpenses = request.MaxTravelExpenses;
                        contract.VacationAge = request.VacationAge;
                        contract.VacationDays = request.VacationDays;
                        contract.Atv = request.Atv;
                        contract.VacationAllowance = request.VacationAllowance;

                        contract.CompanyName = request.CompanyName;
                        contract.EmployerName = request.EmployerName;
                        contract.CompanyAddress = request.CompanyAddress;
                        contract.CompanyPostcode = request.CompanyPostcode;
                        contract.CompanyCity = request.CompanyCity;
                        contract.CompanyPhoneNumber = request.CompanyPhoneNumber;
                        contract.CompanyBtw = request.CompanyBtw;
                        contract.CompanyKvk = request.CompanyKvk;

                        // (Optional) increase ReleaseVersion
                        contract.ReleaseVersion = (contract.ReleaseVersion ?? 0) + 1;

                        // 7) Save changes
                        await db.SaveChangesAsync();

                        // 8) Return updated contract details
                        var responseData = new
                        {
                            contract.Id,
                            contract.CompanyId,
                            contract.DriverId,
                            contract.EmployeeFirstName,
                            contract.EmployeeLastName,
                            contract.ReleaseVersion
                        };

                        return ApiResponseFactory.Success(responseData);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[EmployeeContracts PUT] Error: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the EmployeeContract.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/employee-contracts/{id:guid}/public",
                async (
                    Guid id,
                    [FromQuery] string? access,
                    ApplicationDbContext db
                ) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(access))
                        {
                            return ApiResponseFactory.Error("Please provide access code.",
                                StatusCodes.Status400BadRequest);
                        }

                        var contract = await db.EmployeeContracts
                            .Include(ec => ec.Driver)
                            .ThenInclude(d => d.User)
                            .Include(ec => ec.Company)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ec => ec.Id == id);

                        // Return generic 401 if contract doesn't exist or code doesn't match (case-insensitive)
                        if (contract == null || !string.Equals(contract.AccessCode, access,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return ApiResponseFactory.Error(
                                "The contract wasn't found or the access codes don't match.",
                                StatusCodes.Status401Unauthorized);
                        }

                        var contractResponse = new
                        {
                            contract.Id,
                            Driver = contract.Driver == null
                                ? null
                                : new
                                {
                                    contract.Driver.Id,
                                    FullName = contract.Driver.User.FirstName + " " + contract.Driver.User.LastName,
                                    contract.Driver.AspNetUserId
                                },
                            Company = contract.Company == null
                                ? null
                                : new
                                {
                                    contract.Company.Id,
                                    contract.Company.Name
                                },
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
                            contract.CompanyKvk,
                            contract.Status
                        };

                        return ApiResponseFactory.Success(contractResponse);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Public EmployeeContract GET] Error: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "Unexpected error while accessing contract with access code.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapPost("/employee-contracts/sign",
                    async (
                        [FromForm] SignContractRequest request,
                        HttpContext http,
                        ApplicationDbContext db,
                        IConfiguration config
                    ) =>
                    {
                        try
                        {
                            var remoteIp = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                            var userAgent = http.Request.Headers["User-Agent"].ToString();
                            Console.WriteLine(
                                $"[Contract SIGN] IP={remoteIp}, UserAgent={userAgent}, ContractID={request.ContractId}");

                            if (!Guid.TryParse(request.ContractId, out var parsedContractId))
                            {
                                return ApiResponseFactory.Error("Invalid contractId format.",
                                    StatusCodes.Status400BadRequest);
                            }

                            var contract =
                                await db.EmployeeContracts.FirstOrDefaultAsync(ec => ec.Id == parsedContractId);

                            if (contract == null ||
                                !string.Equals(contract.AccessCode, request.AccessCode,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return ApiResponseFactory.Error("Unauthorized access.",
                                    StatusCodes.Status401Unauthorized);
                            }

                            if (contract.Status == EmployeeContractStatus.Signed)
                            {
                                return ApiResponseFactory.Error("This contract has already been signed.",
                                    StatusCodes.Status400BadRequest);
                            }

                            contract.Status = EmployeeContractStatus.Signed;
                            contract.SignedAt = DateTime.UtcNow;
                            contract.SignedByIp = remoteIp;
                            contract.SignedUserAgent = userAgent;
                            contract.SignatureText = request.Signature;

                            if (request.PdfFile is { Length: > 0 })
                            {
                                var basePath = config["Storage:SignedContractsPath"];
                                if (string.IsNullOrWhiteSpace(basePath))
                                {
                                    return ApiResponseFactory.Error(
                                        "Storage path for signed contracts is not configured.",
                                        StatusCodes.Status500InternalServerError);
                                }

                                Directory.CreateDirectory(basePath);
                                var fileName = $"{contract.Id}.pdf";
                                var filePath = Path.Combine(basePath, fileName);

                                await using var stream = new FileStream(filePath, FileMode.Create);
                                await request.PdfFile.CopyToAsync(stream);
                                Console.WriteLine($"PDF saved to {filePath}");

                                contract.SignedFileName = fileName;
                            }

                            await db.SaveChangesAsync();

                            var responseData = new
                            {
                                contract.Id,
                                contract.Status,
                                contract.SignedAt,
                            };
                            return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[employee-contracts/sign] Error: {ex.Message}");
                            return ApiResponseFactory.Error(
                                "Unexpected error while signing the contract.",
                                StatusCodes.Status500InternalServerError);
                        }
                    })
                .AllowAnonymous()
                .DisableAntiforgery();

            app.MapGet("/employee-contracts/{id:guid}/download", async (
                Guid id,
                [FromQuery] string? access,
                IConfiguration config,
                ApplicationDbContext db) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(access))
                    {
                        return ApiResponseFactory.Error("Access code is required.", StatusCodes.Status401Unauthorized);
                    }

                    var contract = await db.EmployeeContracts.FirstOrDefaultAsync(ec => ec.Id == id);
                    if (contract == null ||
                        !string.Equals(contract.AccessCode, access, StringComparison.OrdinalIgnoreCase))
                    {
                        return ApiResponseFactory.Error("Unauthorized access.", StatusCodes.Status401Unauthorized);
                    }

                    if (contract.Status != EmployeeContractStatus.Signed ||
                        string.IsNullOrWhiteSpace(contract.SignedFileName))
                    {
                        return ApiResponseFactory.Error("Contract is not signed or file is missing.",
                            StatusCodes.Status404NotFound);
                    }

                    var basePath = config["Storage:SignedContractsPath"];
                    if (string.IsNullOrWhiteSpace(basePath))
                    {
                        return ApiResponseFactory.Error("Storage path is not configured.",
                            StatusCodes.Status500InternalServerError);
                    }

                    var filePath = Path.Combine(basePath, contract.SignedFileName);
                    if (!System.IO.File.Exists(filePath))
                    {
                        return ApiResponseFactory.Error("Signed contract file not found.",
                            StatusCodes.Status404NotFound);
                    }

                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileBase64 = Convert.ToBase64String(fileBytes);

                    var contractResponse = new
                    {
                        contract.Id,
                        contract.EmployeeFirstName,
                        contract.EmployeeLastName,
                        contract.Status,
                        FileName = contract.SignedFileName,
                        ContentBase64 = fileBase64
                    };

                    return ApiResponseFactory.Success(contractResponse);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Download Contract] Error: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while downloading the contract.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

            app.MapPost("/employee-contracts/send-sign-mail",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    SendSignMailRequest request,
                    ClaimsPrincipal currentUser,
                    UserManager<ApplicationUser> userManager,
                    ApplicationDbContext db,
                    IConfiguration config,
                    IEmailService emailService) =>
                {
                    try
                    {
                        // ── 1. Basic payload checks ─────────────────────────────────────────
                        if (!Guid.TryParse(request.ContractId, out var contractGuid))
                            return ApiResponseFactory.Error("Invalid contractId format.");

                        if (string.IsNullOrWhiteSpace(request.Email))
                            return ApiResponseFactory.Error("Target e‑mail address is required.");

                        // ── 2. Load contract (incl. company) ───────────────────────────────
                        var contract = await db.EmployeeContracts
                            .Include(c => c.Company)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == contractGuid);

                        if (contract is null)
                            return ApiResponseFactory.Error("EmployeeContract not found.",
                                StatusCodes.Status404NotFound);


                        // ── 3. Authorisation for customerAdmin ─────────────────────────────
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        if (isCustomerAdmin && contract.CompanyId is not null)
                        {
                            var userId = userManager.GetUserId(currentUser);

                            // contact‑person record (incl. companies)
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson is null ||
                                !contactPerson.ContactPersonClientCompanies
                                    .Any(cc => cc.CompanyId == contract.CompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorised to send mails for this company.",
                                    StatusCodes.Status403Forbidden);
                            }
                        }

                        // ── 4. Compose the mail ────────────────────────────────────────────
                        var frontendBaseUrl = config["FrontEnd:BaseURL"]?.TrimEnd('/');
                        // e.g. https://app.example.com/contract
                        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
                            return ApiResponseFactory.Error(
                                "FrontEnd:BaseURL is not configured on the server.",
                                StatusCodes.Status500InternalServerError);

                        var signUrl =
                            $"{frontendBaseUrl}/sign-contract/{contract.Id}?access={Uri.EscapeDataString(contract.AccessCode!)}";


                        var subject = "Please sign your employment contract";
                        var body = $@"
                            <p>Hi,</p>
                            <p>Please review and sign your contract by clicking the link below:</p>
                            <p><a href=""{signUrl}"">{signUrl}</a></p>
                            <p>Your access code: <strong>{contract.AccessCode}</strong></p>
                            <p>Kind regards,<br />The HR team</p>";

                        await emailService.SendEmailAsync(request.Email, subject, body);

                        // ── 5. Done ─────────────────────────────────────────────────────────
                        return ApiResponseFactory.Success(new
                        {
                            contract.Id,
                            SentTo = request.Email,
                            SignUrl = signUrl
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[employee-contracts/send-sign-mail] {ex}");
                        return ApiResponseFactory.Error("Failed to send sign‑request e‑mail.",
                            StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }
}