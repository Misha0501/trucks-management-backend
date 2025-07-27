using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Enums;
using TruckManagement.Helpers;
using TruckManagement.Services;

namespace TruckManagement.Endpoints
{
    public static class DriversEndpoints
    {
        public static void MapDriversEndpoints(this WebApplication app)
        {
            app.MapPost("/drivers/create-with-contract",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    [FromBody] CreateDriverWithContractRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    RoleManager<ApplicationRole> roleManager,
                    ClaimsPrincipal currentUser,
                    DriverCompensationService compensationService
                ) =>
                {
                                    // 0. Validate request object is not null (model binding check)
                if (request == null)
                {
                    return ApiResponseFactory.Error("Invalid request data format.", StatusCodes.Status400BadRequest);
                }

                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // 1. Validate required fields
                        if (string.IsNullOrWhiteSpace(request.Email) ||
                            string.IsNullOrWhiteSpace(request.Password) ||
                            string.IsNullOrWhiteSpace(request.FirstName) ||
                            string.IsNullOrWhiteSpace(request.LastName) ||
                            string.IsNullOrWhiteSpace(request.CompanyId) ||
                            string.IsNullOrWhiteSpace(request.Function))
                        {
                            return ApiResponseFactory.Error(
                                "Required fields missing: Email, Password, FirstName, LastName, CompanyId, Function are required.",
                                StatusCodes.Status400BadRequest);
                        }

                        // 2. Validate Company ID format and existence
                        if (!Guid.TryParse(request.CompanyId, out Guid companyGuid))
                        {
                            return ApiResponseFactory.Error("Invalid Company ID format.", StatusCodes.Status400BadRequest);
                        }

                        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyGuid);
                        if (company == null)
                        {
                            return ApiResponseFactory.Error("Company not found.", StatusCodes.Status400BadRequest);
                        }

                        // 3. Authorization check
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        if (isCustomerAdmin)
                        {
                            // Verify customerAdmin can assign to this company
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person profile not found.", StatusCodes.Status403Forbidden);
                            }

                            var customerAdminCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .ToList();

                            if (!customerAdminCompanyIds.Contains(companyGuid))
                            {
                                return ApiResponseFactory.Error("You can only create drivers for your associated companies.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // 4. Check if email already exists
                        var existingUser = await userManager.FindByEmailAsync(request.Email);
                        if (existingUser != null)
                        {
                            return ApiResponseFactory.Error("A user with this email already exists.", StatusCodes.Status400BadRequest);
                        }

                        // 5. Create ApplicationUser
                        var user = new ApplicationUser
                        {
                            UserName = request.Email,
                            Email = request.Email,
                            FirstName = request.FirstName,
                            LastName = request.LastName,
                            Address = request.Address,
                            PhoneNumber = request.PhoneNumber,
                            Postcode = request.Postcode,
                            City = request.City,
                            Country = request.Country,
                            Remark = request.Remark,
                            IsApproved = true
                        };

                        var createUserResult = await userManager.CreateAsync(user, request.Password);
                        if (!createUserResult.Succeeded)
                        {
                            var errors = createUserResult.Errors.Select(e => e.Description).ToList();
                            return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                        }

                        // 6. Assign driver role
                        var assignRoleResult = await userManager.AddToRoleAsync(user, "driver");
                        if (!assignRoleResult.Succeeded)
                        {
                            await userManager.DeleteAsync(user);
                            var roleErrors = assignRoleResult.Errors.Select(e => e.Description).ToList();
                            return ApiResponseFactory.Error(roleErrors, StatusCodes.Status400BadRequest);
                        }

                        // 7. Create Driver entity
                        var driver = new Driver
                        {
                            Id = Guid.NewGuid(),
                            AspNetUserId = user.Id,
                            CompanyId = companyGuid
                        };
                        db.Drivers.Add(driver);

                        // 8. Create EmployeeContract with auto-filled company data
                        var contract = new EmployeeContract
                        {
                            Id = Guid.NewGuid(),
                            DriverId = driver.Id,
                            CompanyId = companyGuid,
                            ReleaseVersion = 1.0m,
                            Status = EmployeeContractStatus.Pending,
                            
                            // Required contract fields
                            DateOfEmployment = request.DateOfEmployment,
                            WorkweekDuration = request.WorkweekDuration,
                            Function = request.Function,
                            
                            // Optional contract fields
                            DateOfBirth = request.DateOfBirth,
                            ProbationPeriod = request.ProbationPeriod,
                            WeeklySchedule = request.WeeklySchedule,
                            WorkingHours = request.WorkingHours,
                            NoticePeriod = request.NoticePeriod,
                            PayScale = request.PayScale,
                            PayScaleStep = request.PayScaleStep,
                            Bsn = request.BSN,
                            LastWorkingDay = request.LastWorkingDay,
                            VacationDays = request.VacationDays,
                            VacationAge = request.VacationAge,
                            WorkweekDurationPercentage = request.WorkweekDurationPercentage,
                            
                            // Allowances & Settings
                            NightHoursAllowed = request.NightHoursAllowed,
                            KilometersAllowanceAllowed = request.KilometersAllowanceAllowed,
                            CommuteKilometers = request.CommuteKilometers,
                            
                            // Compensation Details
                            CompensationPerMonthExclBtw = request.CompensationPerMonthExclBtw,
                            CompensationPerMonthInclBtw = request.CompensationPerMonthInclBtw,
                            HourlyWage100Percent = request.HourlyWage100Percent,
                            DeviatingWage = request.DeviatingWage,
                            
                            // Travel & Expenses
                            TravelExpenses = request.TravelExpenses,
                            MaxTravelExpenses = request.MaxTravelExpenses,
                            
                            // Vacation Benefits
                            Atv = request.Atv,
                            VacationAllowance = request.VacationAllowance,
                            
                            // Auto-filled company data (can be overridden by request)
                            CompanyName = company.Name,
                            CompanyAddress = company.Address,
                            CompanyPostcode = company.Postcode,
                            CompanyCity = company.City,
                            CompanyPhoneNumber = company.PhoneNumber,
                            EmployerName = !string.IsNullOrWhiteSpace(request.EmployerName) ? request.EmployerName : company.Name,
                            CompanyBtw = request.CompanyBtw,
                            CompanyKvk = request.CompanyKvk,
                            
                            // Employee data from user
                            EmployeeFirstName = user.FirstName,
                            EmployeeLastName = user.LastName,
                            EmployeeAddress = user.Address,
                            EmployeePostcode = user.Postcode,
                            EmployeeCity = user.City
                        };
                        db.EmployeeContracts.Add(contract);

                        // 9. Create default DriverCompensationSettings
                        await compensationService.CreateDefaultDriverCompensationSettingsAsync(driver);

                        // 10. Save all changes
                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // 11. Return success response
                        return ApiResponseFactory.Success(new
                        {
                            UserId = user.Id,
                            DriverId = driver.Id,
                            ContractId = contract.Id,
                            Email = user.Email,
                            FullName = $"{user.FirstName} {user.LastName}",
                            CompanyName = company.Name,
                            ContractStatus = contract.Status.ToString(),
                            DateOfEmployment = contract.DateOfEmployment,
                            Function = contract.Function,
                            WorkweekDuration = contract.WorkweekDuration
                        }, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while creating the driver.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/{driverId}/with-contract",
                [Authorize(Roles = "globalAdmin, customerAdmin, driver")]
                async (
                    Guid driverId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Get current user info for authorization
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                        bool isDriver = currentUser.IsInRole("driver");

                        // Query driver with all related data
                        var driverQuery = db.Drivers
                            .AsNoTracking()
                            .Include(d => d.User)
                            .Include(d => d.Company)
                            .Include(d => d.Car)
                            .Where(d => d.Id == driverId && !d.IsDeleted);

                        // Authorization check for non-global admins
                        if (!isGlobalAdmin)
                        {
                            if (isDriver)
                            {
                                // Drivers can only see their own data
                                driverQuery = driverQuery.Where(d => d.AspNetUserId == currentUserId);
                            }
                            else if (isCustomerAdmin)
                            {
                                // Customer admins can only see drivers from their associated companies
                                var contactPerson = await db.ContactPersons
                                    .Include(cp => cp.ContactPersonClientCompanies)
                                    .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                                if (contactPerson == null)
                                {
                                    return ApiResponseFactory.Error("Contact person profile not found.", StatusCodes.Status403Forbidden);
                                }

                                var customerAdminCompanyIds = contactPerson.ContactPersonClientCompanies
                                    .Where(cpc => cpc.CompanyId.HasValue)
                                    .Select(cpc => cpc.CompanyId.Value)
                                    .ToList();

                                driverQuery = driverQuery.Where(d => d.CompanyId.HasValue && customerAdminCompanyIds.Contains(d.CompanyId.Value));
                            }
                        }

                        var driver = await driverQuery.FirstOrDefaultAsync();
                        
                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found or access denied.", StatusCodes.Status404NotFound);
                        }

                        // Get the employee contract
                        var contract = await db.EmployeeContracts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.DriverId == driver.Id);

                        // Build the comprehensive response
                        var response = new DriverWithContractDto
                        {
                            // User Information
                            UserId = driver.User.Id,
                            Email = driver.User.Email ?? "",
                            FirstName = driver.User.FirstName ?? "",
                            LastName = driver.User.LastName ?? "",
                            Address = driver.User.Address,
                            PhoneNumber = driver.User.PhoneNumber,
                            Postcode = driver.User.Postcode,
                            City = driver.User.City,
                            Country = driver.User.Country,
                            Remark = driver.User.Remark,
                            IsApproved = driver.User.IsApproved,

                            // Driver Information
                            DriverId = driver.Id,
                            CompanyId = driver.CompanyId,
                            CompanyName = driver.Company?.Name,
                            CarId = driver.CarId,
                            CarLicensePlate = driver.Car?.LicensePlate,
                            CarVehicleYear = driver.Car?.VehicleYear,
                            CarRegistrationDate = driver.Car?.RegistrationDate,

                            // Contract Information (if exists)
                            ContractId = contract?.Id,
                            ContractStatus = contract?.Status.ToString() ?? "No Contract",
                            ReleaseVersion = contract?.ReleaseVersion,

                            // Personal Details
                            DateOfBirth = contract?.DateOfBirth,
                            BSN = contract?.Bsn,

                            // Employment Details
                            DateOfEmployment = contract?.DateOfEmployment,
                            LastWorkingDay = contract?.LastWorkingDay,
                            Function = contract?.Function,
                            ProbationPeriod = contract?.ProbationPeriod,
                            WorkweekDuration = contract?.WorkweekDuration,
                            WorkweekDurationPercentage = contract?.WorkweekDurationPercentage,
                            WeeklySchedule = contract?.WeeklySchedule,
                            WorkingHours = contract?.WorkingHours,
                            NoticePeriod = contract?.NoticePeriod,

                            // Work Allowances & Settings
                            NightHoursAllowed = contract?.NightHoursAllowed,
                            KilometersAllowanceAllowed = contract?.KilometersAllowanceAllowed,
                            CommuteKilometers = contract?.CommuteKilometers,

                            // Compensation Details
                            PayScale = contract?.PayScale,
                            PayScaleStep = contract?.PayScaleStep,
                            CompensationPerMonthExclBtw = contract?.CompensationPerMonthExclBtw,
                            CompensationPerMonthInclBtw = contract?.CompensationPerMonthInclBtw,
                            HourlyWage100Percent = contract?.HourlyWage100Percent,
                            DeviatingWage = contract?.DeviatingWage,

                            // Travel & Expenses
                            TravelExpenses = contract?.TravelExpenses,
                            MaxTravelExpenses = contract?.MaxTravelExpenses,

                            // Vacation & Benefits
                            VacationAge = contract?.VacationAge,
                            VacationDays = contract?.VacationDays,
                            Atv = contract?.Atv,
                            VacationAllowance = contract?.VacationAllowance,

                            // Company Details (from contract)
                            EmployerName = contract?.EmployerName,
                            CompanyAddress = contract?.CompanyAddress,
                            CompanyPostcode = contract?.CompanyPostcode,
                            CompanyCity = contract?.CompanyCity,
                            CompanyPhoneNumber = contract?.CompanyPhoneNumber,
                            CompanyBtw = contract?.CompanyBtw,
                            CompanyKvk = contract?.CompanyKvk,

                            // Contract Signing Info
                            AccessCode = contract?.AccessCode,
                            SignedAt = contract?.SignedAt,
                            SignedFileName = contract?.SignedFileName,

                            // Timestamps - using a reasonable default since we don't track creation time
                            CreatedAt = contract?.DateOfEmployment ?? DateTime.UtcNow
                        };

                        return ApiResponseFactory.Success(response, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while retrieving driver information.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapPut("/drivers/{driverId}/with-contract",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid driverId,
                    [FromBody] UpdateDriverWithContractRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    // 0. Validate request object is not null
                    if (request == null)
                    {
                        return ApiResponseFactory.Error("Invalid request data format.", StatusCodes.Status400BadRequest);
                    }

                    using var transaction = await db.Database.BeginTransactionAsync();
                    try
                    {
                        // 1. Get current user info for authorization
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // 2. Find the driver with all related data
                        var driver = await db.Drivers
                            .Include(d => d.User)
                            .Include(d => d.Company)
                            .Include(d => d.Car)
                            .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }

                        // 3. Authorization check for customerAdmin
                        if (!isGlobalAdmin)
                        {
                            // Customer admins can only edit drivers from their associated companies
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person profile not found.", StatusCodes.Status403Forbidden);
                            }

                            var customerAdminCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .ToList();

                            if (!driver.CompanyId.HasValue || !customerAdminCompanyIds.Contains(driver.CompanyId.Value))
                            {
                                return ApiResponseFactory.Error("You do not have permission to edit this driver.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // 4. Validate new company if CompanyId is provided
                        Company? newCompany = null;
                        if (!string.IsNullOrWhiteSpace(request.CompanyId))
                        {
                            if (!Guid.TryParse(request.CompanyId, out var newCompanyId))
                            {
                                return ApiResponseFactory.Error("Invalid CompanyId format.", StatusCodes.Status400BadRequest);
                            }

                            newCompany = await db.Companies.FirstOrDefaultAsync(c => c.Id == newCompanyId);
                            if (newCompany == null)
                            {
                                return ApiResponseFactory.Error("Specified company not found.", StatusCodes.Status400BadRequest);
                            }

                            // Additional authorization check for customerAdmin when changing company
                            if (!isGlobalAdmin)
                            {
                                var contactPerson = await db.ContactPersons
                                    .Include(cp => cp.ContactPersonClientCompanies)
                                    .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                                var customerAdminCompanyIds = contactPerson.ContactPersonClientCompanies
                                    .Where(cpc => cpc.CompanyId.HasValue)
                                    .Select(cpc => cpc.CompanyId.Value)
                                    .ToList();

                                if (!customerAdminCompanyIds.Contains(newCompanyId))
                                {
                                    return ApiResponseFactory.Error("You do not have permission to assign drivers to this company.", StatusCodes.Status403Forbidden);
                                }
                            }
                        }

                        // 5. Validate and handle car assignment
                        Car? newCar = null;
                        if (!string.IsNullOrWhiteSpace(request.CarId))
                        {
                            if (!Guid.TryParse(request.CarId, out var newCarId))
                            {
                                return ApiResponseFactory.Error("Invalid CarId format.", StatusCodes.Status400BadRequest);
                            }

                            newCar = await db.Cars.FirstOrDefaultAsync(c => c.Id == newCarId);
                            if (newCar == null)
                            {
                                return ApiResponseFactory.Error("Specified car not found.", StatusCodes.Status400BadRequest);
                            }

                            // Check if car is already assigned to another driver
                            var existingAssignment = await db.Drivers
                                .FirstOrDefaultAsync(d => d.CarId == newCarId && d.Id != driverId && !d.IsDeleted);
                            if (existingAssignment != null)
                            {
                                return ApiResponseFactory.Error("This car is already assigned to another driver.", StatusCodes.Status400BadRequest);
                            }

                            // Ensure car belongs to the same company as driver (or new company)
                            var targetCompanyId = newCompany?.Id ?? driver.CompanyId;
                            if (newCar.CompanyId != targetCompanyId)
                            {
                                return ApiResponseFactory.Error("Car must belong to the same company as the driver.", StatusCodes.Status400BadRequest);
                            }
                        }

                        // 6. Update ApplicationUser
                        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != driver.User.Email)
                        {
                            // Check email uniqueness
                            var existingUser = await userManager.FindByEmailAsync(request.Email);
                            if (existingUser != null && existingUser.Id != driver.User.Id)
                            {
                                return ApiResponseFactory.Error("Email address is already in use.", StatusCodes.Status400BadRequest);
                            }
                            driver.User.Email = request.Email;
                            driver.User.UserName = request.Email;
                        }

                        // Update other user fields
                        if (request.FirstName != null) driver.User.FirstName = request.FirstName;
                        if (request.LastName != null) driver.User.LastName = request.LastName;
                        if (request.Address != null) driver.User.Address = request.Address;
                        if (request.PhoneNumber != null) driver.User.PhoneNumber = request.PhoneNumber;
                        if (request.Postcode != null) driver.User.Postcode = request.Postcode;
                        if (request.City != null) driver.User.City = request.City;
                        if (request.Country != null) driver.User.Country = request.Country;
                        if (request.Remark != null) driver.User.Remark = request.Remark;

                        // 7. Update Driver
                        if (newCompany != null) driver.CompanyId = newCompany.Id;
                        if (request.CarId != null)
                        {
                            if (string.IsNullOrWhiteSpace(request.CarId))
                            {
                                driver.CarId = null; // Unassign car
                            }
                            else if (newCar != null)
                            {
                                driver.CarId = newCar.Id; // Assign new car
                            }
                        }

                        // 8. Update or create EmployeeContract
                        var contract = await db.EmployeeContracts.FirstOrDefaultAsync(c => c.DriverId == driver.Id);
                        if (contract == null)
                        {
                            // Create new contract if it doesn't exist
                            contract = new EmployeeContract
                            {
                                Id = Guid.NewGuid(),
                                DriverId = driver.Id,
                                Status = EmployeeContractStatus.Pending,
                                ReleaseVersion = 1.0m
                            };
                            db.EmployeeContracts.Add(contract);
                        }

                        // Update contract fields
                        if (request.DateOfBirth.HasValue) contract.DateOfBirth = request.DateOfBirth.Value;
                        if (request.BSN != null) contract.Bsn = request.BSN;
                        if (request.DateOfEmployment.HasValue) contract.DateOfEmployment = request.DateOfEmployment.Value;
                        if (request.LastWorkingDay.HasValue) contract.LastWorkingDay = request.LastWorkingDay.Value;
                        if (request.Function != null) contract.Function = request.Function;
                        if (request.ProbationPeriod != null) contract.ProbationPeriod = request.ProbationPeriod;
                        if (request.WorkweekDuration.HasValue) contract.WorkweekDuration = request.WorkweekDuration.Value;
                        if (request.WorkweekDurationPercentage.HasValue) contract.WorkweekDurationPercentage = request.WorkweekDurationPercentage.Value;
                        if (request.WeeklySchedule != null) contract.WeeklySchedule = request.WeeklySchedule;
                        if (request.WorkingHours != null) contract.WorkingHours = request.WorkingHours;
                        if (request.NoticePeriod != null) contract.NoticePeriod = request.NoticePeriod;
                        if (request.NightHoursAllowed.HasValue) contract.NightHoursAllowed = request.NightHoursAllowed.Value;
                        if (request.KilometersAllowanceAllowed.HasValue) contract.KilometersAllowanceAllowed = request.KilometersAllowanceAllowed.Value;
                        if (request.CommuteKilometers.HasValue) contract.CommuteKilometers = request.CommuteKilometers.Value;
                        if (request.PayScale != null) contract.PayScale = request.PayScale;
                        if (request.PayScaleStep.HasValue) contract.PayScaleStep = request.PayScaleStep.Value;
                        if (request.CompensationPerMonthExclBtw.HasValue) contract.CompensationPerMonthExclBtw = request.CompensationPerMonthExclBtw.Value;
                        if (request.CompensationPerMonthInclBtw.HasValue) contract.CompensationPerMonthInclBtw = request.CompensationPerMonthInclBtw.Value;
                        if (request.HourlyWage100Percent.HasValue) contract.HourlyWage100Percent = request.HourlyWage100Percent.Value;
                        if (request.DeviatingWage.HasValue) contract.DeviatingWage = request.DeviatingWage.Value;
                        if (request.TravelExpenses.HasValue) contract.TravelExpenses = request.TravelExpenses.Value;
                        if (request.MaxTravelExpenses.HasValue) contract.MaxTravelExpenses = request.MaxTravelExpenses.Value;
                        if (request.VacationAge.HasValue) contract.VacationAge = request.VacationAge.Value;
                        if (request.VacationDays.HasValue) contract.VacationDays = request.VacationDays.Value;
                        if (request.Atv.HasValue) contract.Atv = request.Atv.Value;
                        if (request.VacationAllowance.HasValue) contract.VacationAllowance = request.VacationAllowance.Value;

                        // Update company-specific contract fields (auto-filled from new company if changed)
                        if (newCompany != null)
                        {
                            contract.EmployerName = !string.IsNullOrWhiteSpace(request.EmployerName) ? request.EmployerName : newCompany.Name;
                            contract.CompanyAddress = newCompany.Address;
                            contract.CompanyPostcode = newCompany.Postcode;
                            contract.CompanyCity = newCompany.City;
                            contract.CompanyPhoneNumber = newCompany.PhoneNumber;
                        }
                        else
                        {
                            // Update only if explicitly provided
                            if (request.EmployerName != null) contract.EmployerName = request.EmployerName;
                        }
                        
                        if (request.CompanyBtw != null) contract.CompanyBtw = request.CompanyBtw;
                        if (request.CompanyKvk != null) contract.CompanyKvk = request.CompanyKvk;

                        // 9. Save all changes
                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // 10. Reload driver with fresh data for response
                        var updatedDriver = await db.Drivers
                            .AsNoTracking()
                            .Include(d => d.User)
                            .Include(d => d.Company)
                            .Include(d => d.Car)
                            .FirstOrDefaultAsync(d => d.Id == driverId);

                        var updatedContract = await db.EmployeeContracts
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.DriverId == driver.Id);

                        // 11. Build response (reuse the same logic as GET endpoint)
                        var response = new DriverWithContractDto
                        {
                            // User Information
                            UserId = updatedDriver.User.Id,
                            Email = updatedDriver.User.Email ?? "",
                            FirstName = updatedDriver.User.FirstName ?? "",
                            LastName = updatedDriver.User.LastName ?? "",
                            Address = updatedDriver.User.Address,
                            PhoneNumber = updatedDriver.User.PhoneNumber,
                            Postcode = updatedDriver.User.Postcode,
                            City = updatedDriver.User.City,
                            Country = updatedDriver.User.Country,
                            Remark = updatedDriver.User.Remark,
                            IsApproved = updatedDriver.User.IsApproved,

                            // Driver Information
                            DriverId = updatedDriver.Id,
                            CompanyId = updatedDriver.CompanyId,
                            CompanyName = updatedDriver.Company?.Name,
                            CarId = updatedDriver.CarId,
                            CarLicensePlate = updatedDriver.Car?.LicensePlate,
                            CarVehicleYear = updatedDriver.Car?.VehicleYear,
                            CarRegistrationDate = updatedDriver.Car?.RegistrationDate,

                            // Contract Information
                            ContractId = updatedContract?.Id,
                            ContractStatus = updatedContract?.Status.ToString() ?? "No Contract",
                            ReleaseVersion = updatedContract?.ReleaseVersion,

                            // Personal Details
                            DateOfBirth = updatedContract?.DateOfBirth,
                            BSN = updatedContract?.Bsn,

                            // Employment Details
                            DateOfEmployment = updatedContract?.DateOfEmployment,
                            LastWorkingDay = updatedContract?.LastWorkingDay,
                            Function = updatedContract?.Function,
                            ProbationPeriod = updatedContract?.ProbationPeriod,
                            WorkweekDuration = updatedContract?.WorkweekDuration,
                            WorkweekDurationPercentage = updatedContract?.WorkweekDurationPercentage,
                            WeeklySchedule = updatedContract?.WeeklySchedule,
                            WorkingHours = updatedContract?.WorkingHours,
                            NoticePeriod = updatedContract?.NoticePeriod,

                            // Work Allowances & Settings
                            NightHoursAllowed = updatedContract?.NightHoursAllowed,
                            KilometersAllowanceAllowed = updatedContract?.KilometersAllowanceAllowed,
                            CommuteKilometers = updatedContract?.CommuteKilometers,

                            // Compensation Details
                            PayScale = updatedContract?.PayScale,
                            PayScaleStep = updatedContract?.PayScaleStep,
                            CompensationPerMonthExclBtw = updatedContract?.CompensationPerMonthExclBtw,
                            CompensationPerMonthInclBtw = updatedContract?.CompensationPerMonthInclBtw,
                            HourlyWage100Percent = updatedContract?.HourlyWage100Percent,
                            DeviatingWage = updatedContract?.DeviatingWage,

                            // Travel & Expenses
                            TravelExpenses = updatedContract?.TravelExpenses,
                            MaxTravelExpenses = updatedContract?.MaxTravelExpenses,

                            // Vacation & Benefits
                            VacationAge = updatedContract?.VacationAge,
                            VacationDays = updatedContract?.VacationDays,
                            Atv = updatedContract?.Atv,
                            VacationAllowance = updatedContract?.VacationAllowance,

                            // Company Details (from contract)
                            EmployerName = updatedContract?.EmployerName,
                            CompanyAddress = updatedContract?.CompanyAddress,
                            CompanyPostcode = updatedContract?.CompanyPostcode,
                            CompanyCity = updatedContract?.CompanyCity,
                            CompanyPhoneNumber = updatedContract?.CompanyPhoneNumber,
                            CompanyBtw = updatedContract?.CompanyBtw,
                            CompanyKvk = updatedContract?.CompanyKvk,

                            // Contract Signing Info
                            AccessCode = updatedContract?.AccessCode,
                            SignedAt = updatedContract?.SignedAt,
                            SignedFileName = updatedContract?.SignedFileName,

                            // Timestamps
                            CreatedAt = updatedContract?.DateOfEmployment ?? DateTime.UtcNow
                        };

                        return ApiResponseFactory.Success(response, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the driver.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapDelete("/drivers/{driverId}/with-contract",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid driverId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    using var transaction = await db.Database.BeginTransactionAsync();
                    try
                    {
                        // 1. Get current user info for authorization
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // 2. Find the driver with all related data
                        var driver = await db.Drivers
                            .Include(d => d.User)
                            .Include(d => d.Company)
                            .Include(d => d.Car)
                            .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }

                        // 3. Authorization check for customerAdmin
                        if (!isGlobalAdmin)
                        {
                            // Customer admins can only delete drivers from their associated companies
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Contact person profile not found.", StatusCodes.Status403Forbidden);
                            }

                            var customerAdminCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .ToList();

                            if (!driver.CompanyId.HasValue || !customerAdminCompanyIds.Contains(driver.CompanyId.Value))
                            {
                                return ApiResponseFactory.Error("You do not have permission to delete this driver.", StatusCodes.Status403Forbidden);
                            }
                        }

                        // 4. Handle EmployeeContract (preserve for historical/legal purposes)
                        var contract = await db.EmployeeContracts.FirstOrDefaultAsync(c => c.DriverId == driver.Id);
                        if (contract != null)
                        {
                            // Set LastWorkingDay to today if not already set (employment termination record)
                            if (!contract.LastWorkingDay.HasValue)
                            {
                                contract.LastWorkingDay = DateTime.UtcNow.Date;
                            }
                            // Note: We keep the contract for historical/audit purposes
                            // It will become inaccessible through normal driver queries due to soft delete
                        }

                        // 5. Unassign car from driver (1-1 relationship)
                        if (driver.CarId.HasValue)
                        {
                            driver.CarId = null;
                        }

                        // 6. Soft delete the ApplicationUser
                        driver.User.IsDeleted = true;

                        // 7. Soft delete the Driver
                        driver.IsDeleted = true;

                        // 8. Save all changes
                        await db.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return ApiResponseFactory.Success(
                            new { 
                                DriverId = driverId,
                                Message = "Driver and associated contract terminated successfully.",
                                TerminationDate = DateTime.UtcNow.Date
                            }, 
                            StatusCodes.Status200OK
                        );
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while deleting the driver.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers",
                [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
                async (
                    ApplicationDbContext db,
                    ClaimsPrincipal user,
                    UserManager<ApplicationUser> userManager,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
                ) =>
                {
                    try
                    {
                        // Validate pagination parameters
                        if (pageNumber < 1 || pageSize < 1)
                            return ApiResponseFactory.Error("Page number and page size must be greater than zero.",
                                StatusCodes.Status400BadRequest);

                        // Get the requesting user's ID and roles
                        var currentUserId = userManager.GetUserId(user);
                        var roles = await userManager.GetRolesAsync(await userManager.FindByIdAsync(currentUserId));

                        // Check if the user is a global admin
                        bool isGlobalAdmin = roles.Contains("globalAdmin");

                        // If the user is a global admin, retrieve all drivers
                        if (isGlobalAdmin)
                        {
                            var totalDrivers = await db.Drivers.CountAsync();
                            var drivers = await db.Drivers
                                .AsNoTracking()
                                .Include(d => d.Company)
                                .Include(d => d.Car)
                                .Include(d => d.User)
                                .OrderBy(d => d.User.Email)
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .Select(d => new
                                {
                                    d.Id,
                                    d.CompanyId,
                                    CompanyName = d.Company != null ? d.Company.Name : null,
                                    d.CarId,
                                    CarLicensePlate = d.Car != null ? d.Car.LicensePlate : null,
                                    CarVehicleYear = d.Car != null ? d.Car.VehicleYear : null,
                                    CarRegistrationDate = d.Car != null ? d.Car.RegistrationDate : null,
                                    User = new
                                    {
                                        d.User.Id,
                                        d.User.Email,
                                        d.User.FirstName,
                                        d.User.LastName
                                    }
                                })
                                .ToListAsync();

                            return ApiResponseFactory.Success(new
                            {
                                TotalDrivers = totalDrivers,
                                PageNumber = pageNumber,
                                PageSize = pageSize,
                                Drivers = drivers
                            });
                        }

                        // For non-global admins, check if the user is a contact person
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                        if (contactPerson == null)
                            return ApiResponseFactory.Error("Unauthorized to access drivers.",
                                StatusCodes.Status403Forbidden);

                        // Retrieve associated company IDs for the contact person
                        var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Where(cpc => cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .Distinct()
                            .ToList();

                        // Retrieve drivers for the associated companies
                        var totalAssociatedDrivers = await db.Drivers
                            .Where(d => associatedCompanyIds.Contains(d.CompanyId ?? Guid.Empty))
                            .CountAsync();

                        var associatedDrivers = await db.Drivers
                            .AsNoTracking()
                            .Include(d => d.Company)
                            .Include(d => d.Car)
                            .Include(d => d.User)
                            .Where(d => associatedCompanyIds.Contains(d.CompanyId ?? Guid.Empty))
                            .OrderBy(d => d.User.Email)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(d => new
                            {
                                d.Id,
                                d.CompanyId,
                                CompanyName = d.Company != null ? d.Company.Name : null,
                                d.CarId,
                                CarLicensePlate = d.Car != null ? d.Car.LicensePlate : null,
                                CarVehicleYear = d.Car != null ? d.Car.VehicleYear : null,
                                CarRegistrationDate = d.Car != null ? d.Car.RegistrationDate : null,
                                User = new
                                {
                                    d.User.Id,
                                    d.User.Email,
                                    d.User.FirstName,
                                    d.User.LastName
                                }
                            })
                            .ToListAsync();

                        return ApiResponseFactory.Success(new
                        {
                            TotalDrivers = totalAssociatedDrivers,
                            PageNumber = pageNumber,
                            PageSize = pageSize,
                            Drivers = associatedDrivers
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/periods/current",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    try
                    {
                        // -------------------------------------------------------------------
                        // 1. Resolve driver
                        // -------------------------------------------------------------------
                        var aspUserId = userManager.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver == null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

                        // -------------------------------------------------------------------
                        // 2. Determine current period
                        // -------------------------------------------------------------------
                        var (year, periodNr, _) = DateHelper.GetPeriod(DateTime.UtcNow);
                        var (fromDate, toDate) = DateHelper.GetPeriodDateRange(year, periodNr);

                        // -------------------------------------------------------------------
                        // 3. Fetch WeekApprovals for that period
                        // -------------------------------------------------------------------
                        var weeksQuery = db.WeekApprovals
                            .AsNoTracking()
                            .Include(w => w.PartRides)
                            .Where(w => w.DriverId == driver.Id &&
                                        w.Year == year &&
                                        w.PeriodNr == periodNr);

                        var weekApprovals = await weeksQuery.ToListAsync();

                        // -------------------------------------------------------------------
                        // 4. Build four-week payload (fill empty weeks if needed)
                        // -------------------------------------------------------------------
                        var weeks = Enumerable.Range(1, 4).Select(weekInPeriod =>
                            {
                                int weekNumber = DateHelper.GetWeekNumberOfPeriod(year, periodNr, weekInPeriod);
                                var wa = weekApprovals.FirstOrDefault(w => w.WeekNr == weekNumber);

                                // If week has no WeekApproval record yet, create an empty shell
                                var rides = wa?.PartRides
                                    .OrderByDescending(r => r.Date)
                                    .Select(r => new
                                    {
                                        r.Id,
                                        r.Date,
                                        r.Start,
                                        r.End,
                                        r.TotalKilometers,
                                        r.DecimalHours,
                                        r.Remark,
                                        Status = r.Status
                                    })
                                    .Cast<dynamic>()
                                    .ToList() ?? new List<dynamic>();

                                double totalDecimalHours = rides.Sum(r =>
                                {
                                    var dhProp = r.GetType().GetProperty("DecimalHours")!;
                                    return (double?)dhProp.GetValue(r)! ?? 0;
                                });

                                bool isCurrentWeek = DateHelper.GetIso8601WeekOfYear(DateTime.UtcNow) == weekNumber &&
                                                     DateTime.UtcNow.Year == year;

                                WeekApprovalStatus? status;
                                if (wa?.Status != null)
                                {
                                    status = wa.Status;
                                }
                                else if (isCurrentWeek)
                                {
                                    status = WeekApprovalStatus.PendingAdmin;
                                }
                                else if (wa != null && wa.PartRides.Any())
                                {
                                    status = wa.Status;
                                }
                                else
                                {
                                    status = null;
                                }

                                return new
                                {
                                    WeekInPeriod = weekInPeriod,
                                    WeekNumber = weekNumber,
                                    Status = status,
                                    TotalDecimalHours = Math.Round(totalDecimalHours, 2),
                                    PartRides = rides
                                };
                            })
                            .OrderByDescending(w => w.WeekInPeriod) // newest week first
                            .ToList();

                        // -------------------------------------------------------------------
                        // 5. Return
                        // -------------------------------------------------------------------
                        return ApiResponseFactory.Success(new
                        {
                            Year = year,
                            PeriodNr = periodNr,
                            FromDate = fromDate,
                            ToDate = toDate,
                            Weeks = weeks
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/periods/pending",
                [Authorize(Roles = "driver,globalAdmin")]
                async (
                    UserManager<ApplicationUser> userMgr,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10) =>
                {
                    try
                    {
                        var aspUserId = userMgr.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver == null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

                        /* Weeks that are *not* signed yet */
                        var weeks = WeekApprovalQueryHelper.FilterWeeks(db.WeekApprovals.AsNoTracking(),
                                driver.Id, null)
                            .Where(w => w.Status == WeekApprovalStatus.PendingAdmin
                                        || w.Status == WeekApprovalStatus.PendingDriver);

                        /* Group by period */
                        var grouped = weeks
                            .GroupBy(w => new { w.Year, w.PeriodNr })
                            .Select(g => new
                            {
                                g.Key.Year,
                                g.Key.PeriodNr,
                                Status = g.Any(w => w.Status == WeekApprovalStatus.PendingAdmin)
                                    ? WeekApprovalStatus.PendingAdmin
                                    : WeekApprovalStatus.PendingDriver,
                                FromDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).fromDate,
                                ToDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).toDate
                            });

                        var totalCount = await grouped.CountAsync();
                        var data = await grouped
                            .OrderByDescending(x => x.Year)
                            .ThenByDescending(x => x.PeriodNr)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

                        return ApiResponseFactory.Success(new
                        {
                            pageNumber,
                            pageSize,
                            totalCount,
                            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                            data
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/periods/archived",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10) =>
                {
                    try
                    {
                        // ------------------------------------------------- 1. Resolve driver
                        var aspUserId = userManager.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver is null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

                        // ------------------------------------------------- 2. Current period (to exclude)
                        var (currYear, currPeriodNr, _) = DateHelper.GetPeriod(DateTime.UtcNow);

                        // ------------------------------------------------- 3. Query WeekApprovals
                        var weeks = db.WeekApprovals.AsNoTracking()
                            .Where(w => w.DriverId == driver.Id
                                        && w.Status == WeekApprovalStatus.Signed
                                        // keep only periods strictly *before* the current one
                                        && ((w.Year < currYear) ||
                                            (w.Year == currYear && w.PeriodNr < currPeriodNr)));

                        // ------------------------------------------------- 4. Group by period and keep only FULLY-signed ones
                        var periodsQuery = weeks
                            .GroupBy(w => new { w.Year, w.PeriodNr })
                            .Where(g => g.All(w => w.Status == WeekApprovalStatus.Signed))
                            .Select(g => new
                            {
                                g.Key.Year,
                                g.Key.PeriodNr,
                                Status = WeekApprovalStatus.Signed,
                                // helper to compute date range
                                FromDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).fromDate,
                                ToDate = DateHelper.GetPeriodDateRange(g.Key.Year, g.Key.PeriodNr).toDate
                            });

                        // ------------------------------------------------- 5. Paging
                        var totalCount = await periodsQuery.CountAsync();
                        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                        var data = await periodsQuery
                            .OrderByDescending(p => p.Year)
                            .ThenByDescending(p => p.PeriodNr)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

                        // ------------------------------------------------- 6. Return
                        return ApiResponseFactory.Success(new
                        {
                            pageNumber,
                            pageSize,
                            totalCount,
                            totalPages,
                            data
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            // ---------------------------------------------------------------------------
            //  PERIOD ♦ DETAIL  – built from WeekApprovals
            //  GET /drivers/periods/{periodKey}   (periodKey = "YYYY-P")
            // ---------------------------------------------------------------------------
            app.MapGet("/drivers/periods/{periodKey}",
                [Authorize(Roles = "driver, globalAdmin")]
                async (
                    string periodKey,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db) =>
                {
                    try
                    {
                        // 1) Resolve driver
                        var aspUserId = userManager.GetUserId(currentUser);
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId && !d.IsDeleted);

                        if (driver is null)
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);

                        // 2) Parse periodKey => year‑periodNr
                        var parts = periodKey.Split('-');
                        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) ||
                            !int.TryParse(parts[1], out var periodNr))
                            return ApiResponseFactory.Error("Invalid period key format. Use 'YYYY-P' (e.g., 2024-6).",
                                StatusCodes.Status400BadRequest);

                        var (fromDate, toDate) = DateHelper.GetPeriodDateRange(year, periodNr);

                        // 3) Load WeekApprovals for driver/period
                        var waList = await db.WeekApprovals
                            .AsNoTracking()
                            .Include(w => w.PartRides)
                            .Where(w => w.DriverId == driver.Id &&
                                        w.Year == year &&
                                        w.PeriodNr == periodNr)
                            .ToListAsync();

                        if (!waList.Any())
                            return ApiResponseFactory.Error("Period not found.", StatusCodes.Status404NotFound);

                        // 4) Build week buckets (ensure 4 weeks even if some are missing)
                        double totalDecimalHours = 0;
                        decimal totalEarnings = 0;

                        var weeks = Enumerable.Range(1, 4).Select(weekInPeriod =>
                            {
                                int weekNumber = DateHelper.GetWeekNumberOfPeriod(year, periodNr, weekInPeriod);
                                var wa = waList.FirstOrDefault(w => w.PartRides.Any(pr => pr.WeekNumber == weekNumber));

                                var rides = wa?.PartRides
                                    .OrderByDescending(r => r.Date)
                                    .Select(r => new
                                    {
                                        r.Id,
                                        r.Date,
                                        r.Start,
                                        r.End,
                                        r.TotalKilometers,
                                        r.DecimalHours,
                                        r.TaxFreeCompensation,
                                        r.VariousCompensation,
                                        r.Remark,
                                        r.Status
                                    })
                                    .Cast<dynamic>()
                                    .ToList() ?? new List<dynamic>();

                                // totals
                                double weekHours = rides.Sum(r => (double?)(r.DecimalHours ?? 0) ?? 0);
                                decimal weekEarnings = rides.Sum(r =>
                                {
                                    decimal tfc = (decimal)(r.TaxFreeCompensation ?? 0);
                                    decimal vc = (decimal)(r.VariousCompensation ?? 0);
                                    return tfc + vc;
                                });

                                totalDecimalHours += weekHours;
                                totalEarnings += weekEarnings;

                                bool isCurrentWeek = DateHelper.GetIso8601WeekOfYear(DateTime.UtcNow) == weekNumber &&
                                                     DateTime.UtcNow.Year == year;

                                return new
                                {
                                    WeekInPeriod = weekInPeriod,
                                    WeekNumber = weekNumber,
                                    Status = isCurrentWeek
                                        ? (wa?.Status ?? WeekApprovalStatus.PendingAdmin)
                                        : wa?.Status,
                                    TotalDecimalHours = Math.Round(weekHours, 2),
                                    PartRides = rides
                                };
                            })
                            .OrderByDescending(w => w.WeekInPeriod)
                            .ToList();

                        return ApiResponseFactory.Success(new
                        {
                            Year = year,
                            PeriodNr = periodNr,
                            FromDate = fromDate,
                            ToDate = toDate,
                            TotalDecimalHours = Math.Round(totalDecimalHours, 2),
                            TotalEarnings = Math.Round(totalEarnings, 2),
                            Weeks = weeks
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/drivers/week/details",
                [Authorize(Roles = "driver")] async (
                    [FromQuery] int? year,
                    [FromQuery] int? weekNumber,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db
                ) =>
                {
                    try
                    {
                        if (!year.HasValue || !weekNumber.HasValue || year <= 0 || weekNumber <= 0)
                        {
                            return ApiResponseFactory.Error(
                                "Query parameters 'year' and 'weekNumber' are required and must be greater than zero.",
                                StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        var driver =
                            await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);
                        }

                        var query = db.WeekApprovals
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Car)
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Client)
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Company)
                            .Include(wa => wa.PartRides)
                            .ThenInclude(pr => pr.Driver)
                            .ThenInclude(d => d.User)
                            .Where(wa => wa.Year == year.Value && wa.WeekNr == weekNumber.Value)
                            .Where(wa =>
                                wa.Status == WeekApprovalStatus.Signed ||
                                wa.Status == WeekApprovalStatus.PendingDriver);

                        query = query.Where(wa => wa.DriverId == driver.Id);

                        var weekApproval = await query.FirstOrDefaultAsync();

                        if (weekApproval == null)
                        {
                            return ApiResponseFactory.Error("Week not found or not accessible.",
                                StatusCodes.Status404NotFound);
                        }

                        DateTime weekStart = ISOWeek.ToDateTime(year.Value, weekNumber.Value, DayOfWeek.Monday);
                        DateTime weekEnd = weekStart.AddDays(6);

                        var rides = weekApproval.PartRides
                            .Select(pr => new
                            {
                                pr.Id,
                                pr.Date,
                                pr.Start,
                                pr.End,
                                pr.TotalKilometers,
                                pr.DecimalHours,
                                pr.Remark,
                                Car = pr.Car != null ? new { pr.Car.Id, pr.Car.LicensePlate } : null,
                                Client = pr.Client != null ? new { pr.Client.Id, pr.Client.Name } : null,
                                Company = pr.Company != null ? new { pr.Company.Id, pr.Company.Name } : null
                            }).ToList();

                        double totalCompensation = weekApproval.PartRides.Sum(pr =>
                            pr.TaxFreeCompensation + pr.NightAllowance + pr.KilometerReimbursement + pr.ConsignmentFee +
                            pr.VariousCompensation);

                        // TODO: Update vacation hours 
                        return ApiResponseFactory.Success(new
                        {
                            weekApprovalId = weekApproval.Id,
                            week = weekNumber.Value,
                            year = year.Value,
                            startDate = weekStart,
                            endDate = weekEnd,
                            status = weekApproval.Status,
                            vacationHoursLeft = 0,
                            vacationHoursTaken = 0,
                            totalCompensation = Math.Round(totalCompensation, 2),
                            totalHoursWorked = Math.Round(weekApproval.PartRides.Sum(pr => pr.DecimalHours ?? 0), 2),
                            rides
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapPost("/drivers/week/sign",
                [Authorize(Roles = "driver")] async (
                    [FromQuery] int year,
                    [FromQuery] int weekNumber,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    ApplicationDbContext db
                ) =>
                {
                    try
                    {
                        if (year <= 0 || weekNumber <= 0)
                        {
                            return ApiResponseFactory.Error(
                                "Query parameters 'year' and 'weekNumber' are required and must be greater than zero.",
                                StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        var driver =
                            await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == userId && !d.IsDeleted);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver profile not found.",
                                StatusCodes.Status403Forbidden);
                        }

                        var weekApproval = await db.WeekApprovals
                            .FirstOrDefaultAsync(wa =>
                                wa.DriverId == driver.Id &&
                                wa.Year == year &&
                                wa.WeekNr == weekNumber &&
                                wa.Status == WeekApprovalStatus.PendingDriver);

                        if (weekApproval == null)
                        {
                            return ApiResponseFactory.Error("Week not found or not eligible for signing.",
                                StatusCodes.Status404NotFound);
                        }

                        weekApproval.Status = WeekApprovalStatus.Signed;
                        weekApproval.DriverSignedAt = DateTime.UtcNow;

                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success(new
                        {
                            Message = "Week signed successfully.",
                            WeekApprovalId = weekApproval.Id,
                            Status = weekApproval.Status,
                            DriverSignedAt = weekApproval.DriverSignedAt
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally log the exception here
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the PartRide.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}