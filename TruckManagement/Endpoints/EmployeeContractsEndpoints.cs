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
                            ReleaseVersion = request.ReleaseVersion,

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
        }
    }
}