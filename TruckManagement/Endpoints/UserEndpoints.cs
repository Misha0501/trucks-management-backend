using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Models;

namespace TruckManagement.Endpoints;

public static class UserEndpoints
{
    public static WebApplication MapUserEndpoints(this WebApplication app)
    {
        // GET /users/me -> return info about the current authenticated user
        app.MapGet("/users/me", async (
                HttpContext httpContext,
                UserManager<ApplicationUser> userManager,
                ApplicationDbContext dbContext) =>
            {
                // 1) Retrieve user's email claim from the JWT
                var userEmail = httpContext.User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return ApiResponseFactory.Error(
                        "User is not authenticated or no email claim found.",
                        StatusCodes.Status401Unauthorized
                    );
                }

                // 2) Get the user from the database
                var user = await userManager.FindByEmailAsync(userEmail);
                if (user == null)
                {
                    return ApiResponseFactory.Error(
                        "No user found with the provided credentials.",
                        StatusCodes.Status401Unauthorized
                    );
                }

                // 3) Retrieve user roles
                var roles = await userManager.GetRolesAsync(user);

                // 4) Determine role type and load additional properties
                bool isDriver = roles.Contains("driver");
                bool isContactPerson = !isDriver; // Assumes binary role: if not driver, then contact person

                object? driverInfo = null;
                object? contactPersonInfo = null;

                if (isDriver)
                {
                    var driver = await dbContext.Drivers
                        .Include(d => d.Company)
                        .FirstOrDefaultAsync(d => d.AspNetUserId == user.Id);

                    if (driver != null)
                    {
                        driverInfo = new
                        {
                            DriverId = driver.Id,
                            CompanyId = driver.CompanyId,
                            CompanyName = driver.Company?.Name
                        };
                    }
                }

                if (isContactPerson)
                {
                    var contactPerson = await dbContext.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == user.Id);

                    if (contactPerson != null)
                    {
                        var companiesAndClients = await dbContext.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == contactPerson.Id &&
                                          ((cpc.Company != null && !cpc.Company.IsDeleted && cpc.Company.IsApproved) ||
                                           (cpc.Client != null && !cpc.Client.IsDeleted && cpc.Client.IsApproved)))
                            .Select(cpc => new
                            {
                                CompanyId = cpc.CompanyId,
                                CompanyName = cpc.Company.Name,
                                ClientId = cpc.ClientId,
                                ClientName = cpc.Client.Name
                            })
                            .ToListAsync();

                        contactPersonInfo = new
                        {
                            ContactPersonId = contactPerson.Id,
                            clientsCompanies = companiesAndClients
                        };
                    }
                }

                // 5) Prepare the returned data including separate driver and contact person info
                var data = new
                {
                    id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    Postcode = user.Postcode,
                    City = user.City,
                    Country = user.Country,
                    Remark = user.Remark,
                    Roles = roles,
                    DriverInfo = driverInfo,
                    ContactPersonInfo = contactPersonInfo
                };

                // 6) Return a standardized success response
                return ApiResponseFactory.Success(
                    data: data,
                    statusCode: StatusCodes.Status200OK
                );
            })
            .RequireAuthorization();

        app.MapGet("/users/{aspUserId}/driver/compensations",
            [Authorize] async (
                string aspUserId,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                ApplicationDbContext dbContext
            ) =>
            {
                try
                {
                    // 1) Validate aspUserId
                    if (string.IsNullOrWhiteSpace(aspUserId))
                    {
                        return ApiResponseFactory.Error(
                            "Invalid ASP user ID format.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    // 2) Ensure user is authenticated
                    var currentUserId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        return ApiResponseFactory.Error(
                            "User not authenticated.",
                            StatusCodes.Status401Unauthorized
                        );
                    }

                    // 3) Retrieve user roles & check if globalAdmin
                    var currentUserObject = await userManager.GetUserAsync(currentUser);
                    var userRoles = await userManager.GetRolesAsync(currentUserObject);
                    bool isGlobalAdmin = userRoles.Contains("globalAdmin");

                    // 4) Load the driver with compensation by aspUserId
                    var driverEntity = await dbContext.Drivers
                        .Include(d => d.Company)
                        .Include(d => d.DriverCompensationSettings)
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId);

                    if (driverEntity == null)
                    {
                        return ApiResponseFactory.Error(
                            "Driver not found for the given ASP user ID.",
                            StatusCodes.Status404NotFound
                        );
                    }

                    // 5) If not globalAdmin, ensure user is in the same company
                    if (!isGlobalAdmin)
                    {
                        if (!driverEntity.CompanyId.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "Driver has no associated company, access denied.",
                                StatusCodes.Status403Forbidden
                            );
                        }

                        bool belongsToSameCompany = await UserCompanyHelper.CheckUserBelongsToCompanyAsync(
                            currentUserId,
                            driverEntity.CompanyId.Value,
                            dbContext
                        );

                        if (!belongsToSameCompany)
                        {
                            return ApiResponseFactory.Error(
                                "You are not authorized to view this driver's compensation settings.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // 6) Return the compensation data
                    var settings = driverEntity.DriverCompensationSettings;
                    if (settings == null)
                    {
                        return ApiResponseFactory.Error(
                            "No compensation settings found for this driver.",
                            StatusCodes.Status404NotFound
                        );
                    }

                    var responseData = new
                    {
                        driverEntity.Id,
                        driverEntity.AspNetUserId,
                        driverEntity.CompanyId,
                        CompensationSettings = new
                        {
                            settings.PercentageOfWork,
                            settings.NightHoursAllowed,
                            settings.NightHours19Percent,
                            settings.DriverRatePerHour,
                            settings.NightAllowanceRate,
                            settings.KilometerAllowanceEnabled,
                            settings.KilometersOneWayValue,
                            settings.KilometersMin,
                            settings.KilometersMax,
                            settings.KilometerAllowance,
                            settings.Salary4Weeks,
                            settings.WeeklySalary,
                            DateOfEmployment = settings.DateOfEmployment.ToString("yyyy-MM-dd")
                        }
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error fetching driver compensation: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while fetching driver compensation.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );

        app.MapPut("/users/{aspUserId}/driver/compensations",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string aspUserId,
                [FromBody] DriverCompensationSettings request,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                ApplicationDbContext dbContext
            ) =>
            {
                try
                {
                    // Validate user
                    if (string.IsNullOrWhiteSpace(aspUserId))
                    {
                        return ApiResponseFactory.Error("Invalid ASP user ID.", StatusCodes.Status400BadRequest);
                    }

                    var currentUserId = userManager.GetUserId(currentUser);
                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);
                    }

                    // Get roles
                    var user = await userManager.GetUserAsync(currentUser);
                    var userRoles = await userManager.GetRolesAsync(user);
                    var isGlobalAdmin = userRoles.Contains("globalAdmin");

                    // Find driver
                    var driver = await dbContext.Drivers
                        .Include(d => d.Company)
                        .Include(d => d.DriverCompensationSettings)
                        .FirstOrDefaultAsync(d => d.AspNetUserId == aspUserId);

                    if (driver == null)
                    {
                        return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                    }

                    // If not global admin, validate company access
                    if (!isGlobalAdmin)
                    {
                        if (!driver.CompanyId.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "Driver has no associated company.",
                                StatusCodes.Status403Forbidden
                            );
                        }

                        var hasAccess = await UserCompanyHelper.CheckUserBelongsToCompanyAsync(
                            currentUserId,
                            driver.CompanyId.Value,
                            dbContext
                        );

                        if (!hasAccess)
                        {
                            return ApiResponseFactory.Error(
                                "You are not authorized to update this driver's compensation.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // Update or create the settings
                    var settings = driver.DriverCompensationSettings;
                    if (settings == null)
                    {
                        settings = new DriverCompensationSettings
                        {
                            Id = Guid.NewGuid(),
                            DriverId = driver.Id,
                            DateOfEmployment = request.DateOfEmployment
                        };
                        dbContext.DriverCompensationSettings.Add(settings);
                    }

                    // Update fields
                    settings.PercentageOfWork = request.PercentageOfWork;
                    settings.NightHoursAllowed = request.NightHoursAllowed;
                    settings.NightHours19Percent = request.NightHours19Percent;
                    settings.DriverRatePerHour = request.DriverRatePerHour;
                    settings.NightAllowanceRate = request.NightAllowanceRate;
                    settings.KilometerAllowanceEnabled = request.KilometerAllowanceEnabled;
                    settings.KilometersOneWayValue = request.KilometersOneWayValue;
                    settings.KilometersMin = request.KilometersMin;
                    settings.KilometersMax = request.KilometersMax;
                    settings.KilometerAllowance = request.KilometerAllowance;
                    settings.Salary4Weeks = request.Salary4Weeks;
                    settings.WeeklySalary = request.WeeklySalary;
                    settings.DateOfEmployment = DateTime.SpecifyKind(request.DateOfEmployment, DateTimeKind.Utc);

                    await dbContext.SaveChangesAsync();

                    return ApiResponseFactory.Success("Driver compensation settings updated.", StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error updating driver compensation: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while updating driver compensation.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );

        app.MapPost("/users/change-password", async (
                ChangePasswordRequest req,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext
            ) =>
            {
                // 1) Get the user's ID from JWT claims (e.g., "sub")
                //    The GenerateJwtToken typically sets user.Id in JwtRegisteredClaimNames.Sub
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiResponseFactory.Error("Invalid token or user ID not found.",
                        StatusCodes.Status401Unauthorized);
                }

                // 2) Retrieve the user
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);
                }

                // 3) Check new/confirm match
                if (req.NewPassword != req.ConfirmNewPassword)
                {
                    return ApiResponseFactory.Error("New password and confirmation do not match.",
                        StatusCodes.Status400BadRequest);
                }

                // 4) Attempt to change the password
                var result = await userManager.ChangePasswordAsync(user, req.OldPassword, req.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                }

                // 5) Return success
                return ApiResponseFactory.Success("Password changed successfully.", StatusCodes.Status200OK);
            })
            .RequireAuthorization(); // Must be logged in to change password

        // GET /users (Paginated)
        // GET /users => Paginated list of users (with roles)
        app.MapGet("/users",
            [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
            async (
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 1000
            ) =>
            {
                // 1. Retrieve the current user's ID
                var currentUserId = userManager.GetUserId(currentUser);

                // 2. Determine the roles of the current user
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                bool isCustomerAccountant = currentUser.IsInRole("customerAccountant");
                bool isEmployer = currentUser.IsInRole("employer");
                bool isCustomer = currentUser.IsInRole("customer");

                bool isContactPerson = isCustomerAdmin || isCustomerAccountant || isEmployer || isCustomer;

                // 3. If the user is not a globalAdmin or any ContactPerson role, deny access
                if (!isGlobalAdmin && !isContactPerson)
                    return ApiResponseFactory.Error("Unauthorized to view users.", StatusCodes.Status403Forbidden);

                // 4. If the user is a ContactPerson, retrieve their associated company IDs
                List<Guid> contactPersonCompanyIds = new List<Guid>();
                if (isContactPerson)
                {
                    // Retrieve the ContactPerson entity associated with the current user
                    var currentContactPerson = await db.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson != null)
                    {
                        // Retrieve direct company IDs the contact person is associated with
                        var directCompanyIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .Distinct()
                            .ToListAsync();

                        // Retrieve client IDs associated with those companies
                        var clientIdsOfMyCompanies = await db.Clients
                            .Where(client => directCompanyIds.Contains(client.CompanyId))
                            .Select(client => client.Id)
                            .Distinct()
                            .ToListAsync();

                        // Retrieve parent company IDs from those clients
                        var parentCompanyIdsFromClients = await db.Clients
                            .Where(client => clientIdsOfMyCompanies.Contains(client.Id))
                            .Select(client => client.CompanyId)
                            .Distinct()
                            .ToListAsync();

                        // Merge direct company IDs and client-owned parent companies
                        contactPersonCompanyIds = directCompanyIds
                            .Concat(parentCompanyIdsFromClients)
                            .Distinct()
                            .ToList();
                    }
                    else
                    {
                        // If the current user is a ContactPerson but has no associated ContactPerson record
                        return ApiResponseFactory.Error("ContactPerson profile not found.",
                            StatusCodes.Status403Forbidden);
                    }
                }

                // 5. Build the base query based on the user's role
                IQueryable<ApplicationUser> usersQuery = db.Users.AsQueryable();

                if (isContactPerson)
                {
                    usersQuery = usersQuery.Where(u =>
                        // Users who are Drivers associated with ContactPerson's companies
                        db.Drivers.Any(d =>
                            d.AspNetUserId == u.Id && d.CompanyId.HasValue &&
                            contactPersonCompanyIds.Contains(d.CompanyId.Value)) ||

                        // Users who are ContactPersons associated with ContactPerson's companies
                        db.ContactPersonClientCompanies.Any(cpc =>
                            cpc.ContactPerson.AspNetUserId == u.Id &&
                            cpc.CompanyId.HasValue &&
                            contactPersonCompanyIds.Contains(cpc.CompanyId.Value)) ||

                        // Users who are ContactPersons associated with clients owned by ContactPerson's companies
                        db.ContactPersonClientCompanies.Any(cpc =>
                            cpc.ContactPerson.AspNetUserId == u.Id &&
                            cpc.ClientId.HasValue &&
                            db.Clients.Any(client =>
                                client.Id == cpc.ClientId.Value &&
                                contactPersonCompanyIds.Contains(client.CompanyId)))
                    );
                }
                // If globalAdmin, no additional filtering is needed

                // 6. Get total user count after filtering for pagination
                var totalUsers = await usersQuery.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

                // 7. Apply pagination and select necessary fields
                var pagedUsers = await usersQuery
                    .AsNoTracking()
                    .OrderBy(u => u.Email)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        Roles = (from ur in db.UserRoles
                            join r in db.Roles on ur.RoleId equals r.Id
                            where ur.UserId == u.Id
                            select r.Name).ToList(),

                        DriverInfo = (from d in db.Drivers
                            join c in db.Companies on d.CompanyId equals c.Id into compJoin
                            from comp in compJoin.DefaultIfEmpty()
                            where d.AspNetUserId == u.Id
                            select new
                            {
                                DriverId = d.Id,
                                CompanyId = d.CompanyId,
                                CompanyName = comp != null ? comp.Name : null
                            }).FirstOrDefault(),

                        ContactPersonInfo = (from cp in db.ContactPersons
                            where cp.AspNetUserId == u.Id
                            select new
                            {
                                ContactPersonId = cp.Id,
                                ClientsCompanies = (from cpc in db.ContactPersonClientCompanies
                                    where cpc.ContactPersonId == cp.Id
                                    select new
                                    {
                                        cpc.CompanyId,
                                        CompanyName = cpc.Company.Name,
                                        cpc.ClientId,
                                        ClientName = cpc.Client.Name
                                    }).ToList()
                            }).FirstOrDefault()
                    })
                    .ToListAsync();

                // 8. Build the response object with pagination info
                var responseData = new
                {
                    totalUsers,
                    totalPages,
                    pageNumber,
                    pageSize,
                    data = pagedUsers
                };

                // 9. Return success response
                return ApiResponseFactory.Success(
                    responseData,
                    StatusCodes.Status200OK
                );
            });


        app.MapGet("/users/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                // 1. Validate and parse the user ID from the route
                if (!Guid.TryParse(id, out Guid userGuid))
                    return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);

                // 2. Retrieve the target user by ID
                var targetUser = await userManager.FindByIdAsync(userGuid.ToString());
                if (targetUser == null)
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                // 3. Retrieve the current user's roles
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                bool isCustomerAccountant = currentUser.IsInRole("customerAccountant");
                bool isEmployer = currentUser.IsInRole("employer");
                bool isCustomer = currentUser.IsInRole("customer");

                bool isContactPerson = isGlobalAdmin || isCustomerAdmin || isCustomerAccountant || isEmployer ||
                                       isCustomer;

                // 4. If the user is not a globalAdmin or a ContactPerson, deny access
                if (!isGlobalAdmin && !isContactPerson)
                    return ApiResponseFactory.Error("Unauthorized to view users.", StatusCodes.Status403Forbidden);

                // 5. If the user is a ContactPerson, retrieve their associated company IDs
                List<Guid> contactPersonCompanyIds = new List<Guid>();
                if (isContactPerson && !isGlobalAdmin)
                {
                    var currentUserId = userManager.GetUserId(currentUser);
                    // Retrieve the ContactPerson entity associated with the current user
                    var currentContactPerson = await db.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson != null)
                    {
                        // 1) Retrieve direct company IDs from ContactPersonClientCompanies
                        var directCompanyIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .Distinct()
                            .ToListAsync();

                        // 2)Retrieve direct cletn IDs from ContactPersonClientCompanies
                        var directClientIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.ClientId.HasValue)
                            .Select(cpc => cpc.ClientId.Value)
                            .Distinct()
                            .ToListAsync();

                        // 2) Gather client IDs from those companies so that company people can view clients 
                        var companyOwnedClientIds = await db.Clients
                            .Where(client => directCompanyIds.Contains(client.CompanyId))
                            .Select(client => client.Id)
                            .Distinct()
                            .ToListAsync();

                        // 4) directClientParentCompanies for clients to view companies
                        var directClientParentCompanies = await db.Clients
                            .Where(client => directClientIds.Contains(client.Id))
                            .Select(client => client.CompanyId)
                            .Distinct()
                            .ToListAsync();

                        // 5) Combine both sets of client IDs
                        contactPersonCompanyIds = directClientIds
                            .Concat(companyOwnedClientIds)
                            .Concat(directCompanyIds)
                            .Concat(directClientParentCompanies)
                            .Distinct()
                            .ToList();
                    }
                    else
                    {
                        // If the current user is a ContactPerson but has no associated ContactPerson record
                        return ApiResponseFactory.Error("ContactPerson profile not found.",
                            StatusCodes.Status403Forbidden);
                    }
                }

                // 6. If the user is a ContactPerson, verify that the target user is associated with their companies
                if (isContactPerson && !isGlobalAdmin)
                {
                    bool isAuthorized = false;

                    // Check if the target user is a Driver associated with any of the ContactPerson's companies
                    var driver = await db.Drivers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.AspNetUserId == targetUser.Id && d.CompanyId.HasValue);

                    if (driver != null && contactPersonCompanyIds.Contains(driver.CompanyId.Value))
                    {
                        isAuthorized = true;
                    }

                    // Check if the target user is a ContactPerson associated with any of the ContactPerson's companies
                    if (!isAuthorized)
                    {
                        var targetContactPerson = await db.ContactPersons
                            .AsNoTracking()
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == targetUser.Id);

                        if (targetContactPerson != null)
                        {
                            var targetUserCompanyIds = await db.ContactPersonClientCompanies
                                .Where(cpc => cpc.ContactPersonId == targetContactPerson.Id && cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .ToListAsync();

                            var targetUserClientIds = await db.ContactPersonClientCompanies
                                .Where(cpc => cpc.ContactPersonId == targetContactPerson.Id && cpc.ClientId.HasValue)
                                .Select(cpc => cpc.ClientId.Value)
                                .Distinct()
                                .ToListAsync();

                            var sum = targetUserCompanyIds.Concat(targetUserClientIds).Distinct().ToList();

                            if (sum.Any(cId => contactPersonCompanyIds.Contains(cId)))
                            {
                                isAuthorized = true;
                            }
                        }
                    }

                    if (!isAuthorized)
                        return ApiResponseFactory.Error("You are not authorized to view this user.",
                            StatusCodes.Status403Forbidden);
                }

                // 7. Retrieve user roles
                var roles = await userManager.GetRolesAsync(targetUser);

                // 8. Determine role type without assuming
                bool isDriver = roles.Contains("driver");
                bool isContactPersonUser = roles.Any(role =>
                    role.Equals("globalAdmin", StringComparison.OrdinalIgnoreCase)
                    || role.Equals("customerAdmin", StringComparison.OrdinalIgnoreCase)
                    || role.Equals("customerAccountant", StringComparison.OrdinalIgnoreCase)
                    || role.Equals("employer", StringComparison.OrdinalIgnoreCase)
                    || role.Equals("customer", StringComparison.OrdinalIgnoreCase));

                object? driverInfo = null;
                object? contactPersonInfo = null;

                // 9. Load driver information if applicable
                if (isDriver)
                {
                    driverInfo = await db.Drivers
                        .AsNoTracking()
                        .Where(d => d.AspNetUserId == targetUser.Id)
                        .Include(d => d.Company)
                        .Select(d => new
                        {
                            DriverId = d.Id,
                            CompanyId = d.CompanyId,
                            CompanyName = d.Company != null ? d.Company.Name : null
                        })
                        .FirstOrDefaultAsync();
                }

                // 10. Load contact person information if applicable
                if (isContactPersonUser)
                {
                    var contactPerson = await db.ContactPersons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == targetUser.Id);

                    if (contactPerson != null)
                    {
                        var companiesAndClients = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == contactPerson.Id &&
                                          ((cpc.Company != null && !cpc.Company.IsDeleted && cpc.Company.IsApproved) ||
                                           (cpc.Client != null && !cpc.Client.IsDeleted && cpc.Client.IsApproved)))
                            .Select(cpc => new
                            {
                                cpc.CompanyId,
                                CompanyName = cpc.Company.Name,
                                cpc.ClientId,
                                ClientName = cpc.Client.Name
                            })
                            .ToListAsync();

                        contactPersonInfo = new
                        {
                            ContactPersonId = contactPerson.Id,
                            clientsCompanies = companiesAndClients
                        };
                    }
                }

                // 11. Prepare response data
                var data = new
                {
                    targetUser.Id,
                    targetUser.Email,
                    targetUser.FirstName,
                    targetUser.LastName,
                    targetUser.Address,
                    targetUser.PhoneNumber,
                    targetUser.Postcode,
                    targetUser.City,
                    targetUser.Country,
                    targetUser.Remark,
                    Roles = roles,
                    DriverInfo = driverInfo,
                    ContactPersonInfo = contactPersonInfo
                };

                // 12. Return standardized success response
                return ApiResponseFactory.Success(data, StatusCodes.Status200OK);
            }
        );


        app.MapPut("/users/{id}/basic",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                [FromBody] UpdateUserBasicRequest req,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                RoleManager<ApplicationRole> roleManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                // 1. Validate and parse the user ID from the route
                if (!Guid.TryParse(id, out Guid userGuid))
                    return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);

                // 2. Retrieve the target user by ID
                var targetUser = await userManager.FindByIdAsync(userGuid.ToString());
                if (targetUser == null)
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                // 3. Retrieve the current user's ID
                var currentUserId = userManager.GetUserId(currentUser);

                // 4. Determine the roles of the current user
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                // 5. Authorization Check
                if (!isGlobalAdmin && !isCustomerAdmin)
                    return ApiResponseFactory.Error("Unauthorized to update this user.",
                        StatusCodes.Status403Forbidden);

                // 6. If the current user is a "customerAdmin", retrieve their associated company IDs
                List<Guid> customerAdminCompanyIds = new List<Guid>();
                if (isCustomerAdmin)
                {
                    // Retrieve the ContactPerson entity associated with the current user
                    var currentContactPerson = await db.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson != null)
                    {
                        // 1) Retrieve direct company IDs associated with the customer admin
                        var directCompanyIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .Distinct()
                            .ToListAsync();

                        // 2) Retrieve client IDs that the customer admin is associated with
                        var directClientIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.ClientId.HasValue)
                            .Select(cpc => cpc.ClientId.Value)
                            .Distinct()
                            .ToListAsync();

                        // 3) Retrieve the parent company IDs of the associated clients
                        var parentCompanyIds = await db.Clients
                            .Where(cl => directClientIds.Contains(cl.Id))
                            .Select(cl => cl.CompanyId)
                            .Distinct()
                            .ToListAsync();

                        // 4) Merge both direct company IDs and parent company IDs from clients
                        customerAdminCompanyIds = directCompanyIds
                            .Concat(parentCompanyIds)
                            .Distinct()
                            .ToList();
                    }
                    else
                    {
                        // If the current user is a CustomerAdmin but has no associated ContactPerson record
                        return ApiResponseFactory.Error("CustomerAdmin's ContactPerson profile not found.",
                            StatusCodes.Status403Forbidden);
                    }
                }

                // 7. Determine the target user's role and associated companies
                List<Guid> targetUserCompanyIds = new List<Guid>();

                // Check if the target user is a Driver
                var driver = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == targetUser.Id);
                if (driver != null && driver.CompanyId.HasValue)
                {
                    targetUserCompanyIds.Add(driver.CompanyId.Value);
                }

                // Check if the target user is a ContactPerson
                var targetContactPerson =
                    await db.ContactPersons.FirstOrDefaultAsync(cp => cp.AspNetUserId == targetUser.Id);
                if (targetContactPerson != null)
                {
                    var contactPersonCompanyIds = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.ContactPersonId == targetContactPerson.Id && cpc.CompanyId.HasValue)
                        .Select(cpc => cpc.CompanyId.Value)
                        .ToListAsync();

                    // Retrieve client IDs the target user is associated with
                    var contactPersonClientIds = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.ContactPersonId == targetContactPerson.Id && cpc.ClientId.HasValue)
                        .Select(cpc => cpc.ClientId.Value)
                        .ToListAsync();

                    // Retrieve the parent company IDs of the associated clients
                    var parentCompanyIds = await db.Clients
                        .Where(cl => contactPersonClientIds.Contains(cl.Id))
                        .Select(cl => cl.CompanyId)
                        .Distinct()
                        .ToListAsync();

                    targetUserCompanyIds.AddRange(contactPersonCompanyIds);
                    targetUserCompanyIds.AddRange(contactPersonClientIds);
                    targetUserCompanyIds.AddRange(parentCompanyIds);
                }

                // If the target user is neither a Driver nor a ContactPerson, restrict modification
                if (driver == null && targetContactPerson == null && !isGlobalAdmin)
                    return ApiResponseFactory.Error(
                        "Only Drivers or Contact Persons can be modified through this endpoint.",
                        StatusCodes.Status403Forbidden);

                // 8. If the current user is a "customerAdmin", verify the overlap in companies
                if (isCustomerAdmin)
                {
                    bool hasOverlap = targetUserCompanyIds.Any(cId => customerAdminCompanyIds.Contains(cId));

                    if (!hasOverlap)
                        return ApiResponseFactory.Error("You are not authorized to modify this user.",
                            StatusCodes.Status403Forbidden);
                }

                // 9. Begin a database transaction to ensure atomicity
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // 10. Handle role updates only if the requester is a GlobalAdmin
                    if (req.Roles != null && isGlobalAdmin)
                    {
                        if (req.Roles.Count == 0)
                        {
                            // Remove all roles
                            var currentRoles = await userManager.GetRolesAsync(targetUser);
                            if (currentRoles.Count > 0)
                            {
                                var removeAllResult = await userManager.RemoveFromRolesAsync(targetUser, currentRoles);
                                if (!removeAllResult.Succeeded)
                                {
                                    var errors = removeAllResult.Errors.Select(e => e.Description).ToList();
                                    return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                                }
                            }
                        }
                        else
                        {
                            // Validate each role exists
                            foreach (var role in req.Roles)
                            {
                                if (!await roleManager.RoleExistsAsync(role))
                                    return ApiResponseFactory.Error($"Role '{role}' does not exist.",
                                        StatusCodes.Status400BadRequest);
                            }

                            var currentRoles = await userManager.GetRolesAsync(targetUser);
                            var rolesToAdd = req.Roles.Except(currentRoles).ToList();
                            var rolesToRemove = currentRoles.Except(req.Roles).ToList();

                            if (rolesToRemove.Count > 0)
                            {
                                var removeResult = await userManager.RemoveFromRolesAsync(targetUser, rolesToRemove);
                                if (!removeResult.Succeeded)
                                {
                                    var errors = removeResult.Errors.Select(e => e.Description).ToList();
                                    return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                                }
                            }

                            if (rolesToAdd.Count > 0)
                            {
                                var addResult = await userManager.AddToRolesAsync(targetUser, rolesToAdd);
                                if (!addResult.Succeeded)
                                {
                                    var errors = addResult.Errors.Select(e => e.Description).ToList();
                                    return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                                }
                            }
                        }
                    }

                    // 11. Update basic fields
                    if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != targetUser.Email)
                    {
                        var existingEmailUser = await userManager.FindByEmailAsync(req.Email);
                        if (existingEmailUser != null && existingEmailUser.Id != targetUser.Id)
                            return ApiResponseFactory.Error("Email already in use.", StatusCodes.Status400BadRequest);
                        targetUser.Email = req.Email;
                        targetUser.UserName = req.Email; // Assuming UserName is same as Email
                    }

                    if (!string.IsNullOrWhiteSpace(req.FirstName)) targetUser.FirstName = req.FirstName;
                    if (!string.IsNullOrWhiteSpace(req.LastName)) targetUser.LastName = req.LastName;
                    if (!string.IsNullOrWhiteSpace(req.Address)) targetUser.Address = req.Address;
                    if (!string.IsNullOrWhiteSpace(req.PhoneNumber)) targetUser.PhoneNumber = req.PhoneNumber;
                    if (!string.IsNullOrWhiteSpace(req.Postcode)) targetUser.Postcode = req.Postcode;
                    if (!string.IsNullOrWhiteSpace(req.City)) targetUser.City = req.City;
                    if (!string.IsNullOrWhiteSpace(req.Country)) targetUser.Country = req.Country;
                    if (!string.IsNullOrWhiteSpace(req.Remark)) targetUser.Remark = req.Remark;

                    // 12. Save changes to the database
                    var updateResult = await userManager.UpdateAsync(targetUser);
                    if (!updateResult.Succeeded)
                    {
                        var errors = updateResult.Errors.Select(e => e.Description).ToList();
                        return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                    }

                    // 13. Commit the transaction
                    await transaction.CommitAsync();

                    // 14. Retrieve updated roles
                    var updatedRoles = await userManager.GetRolesAsync(targetUser);

                    // 15. Prepare the response payload
                    var updatedUserData = new
                    {
                        targetUser.Id,
                        targetUser.Email,
                        targetUser.FirstName,
                        targetUser.LastName,
                        targetUser.Address,
                        targetUser.PhoneNumber,
                        targetUser.Postcode,
                        targetUser.City,
                        targetUser.Country,
                        targetUser.Remark,
                        Roles = updatedRoles
                    };

                    // 16. Return success response
                    return ApiResponseFactory.Success(updatedUserData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    // 17. Rollback the transaction on error
                    await transaction.RollbackAsync();

                    // 18. Log the exception details to the console
                    Console.WriteLine($"[Error] An exception occurred while updating the user.");
                    Console.WriteLine($"[Error] TargetUserId: {targetUser.Id}");
                    Console.WriteLine($"[Error] Exception Message: {ex.Message}");
                    Console.WriteLine($"[Error] Stack Trace: {ex.StackTrace}");

                    // 19. Return a generic error response to the client
                    return ApiResponseFactory.Error(
                        "An error occurred while updating the user.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        app.MapPut("/users/{id}/driver",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                [FromBody] UpdateDriverRequest req,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                // 1. Validate and parse the user ID from the route
                if (!Guid.TryParse(id, out Guid userGuid))
                    return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);

                // 2. Retrieve the target user by ID
                var targetUser = await userManager.FindByIdAsync(userGuid.ToString());
                if (targetUser == null)
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                // 3. Retrieve the current user's ID
                var currentUserId = userManager.GetUserId(currentUser);

                // 4. Prevent drivers from modifying their own information
                if (currentUserId == targetUser.Id)
                    return ApiResponseFactory.Error("Drivers cannot modify their own information.",
                        StatusCodes.Status403Forbidden);

                // 5. Determine the roles of the current user
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                // 6. If the current user is a "customerAdmin", retrieve their associated company IDs
                List<Guid> customerAdminCompanyIds = new List<Guid>();
                if (isCustomerAdmin)
                {
                    // Retrieve the ContactPerson entity associated with the current user
                    var currentContactPerson = await db.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson != null)
                    {
                        // Retrieve associated CompanyIds from ContactPersonClientCompany
                        customerAdminCompanyIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .ToListAsync();
                    }
                }

                // 7. Retrieve the existing Driver entity for the target user
                var driverEntity = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == targetUser.Id);
                if (driverEntity == null)
                    return ApiResponseFactory.Error("Driver profile not found for this user.",
                        StatusCodes.Status404NotFound);

                // 8. If the current user is a "customerAdmin", verify the driver's current CompanyId
                if (isCustomerAdmin)
                {
                    if (!driverEntity.CompanyId.HasValue ||
                        !customerAdminCompanyIds.Contains(driverEntity.CompanyId.Value))
                        return ApiResponseFactory.Error("You are not authorized to modify this driver's information.",
                            StatusCodes.Status403Forbidden);
                }

                // 9. Begin a database transaction to ensure atomicity
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // 10. Validate and authorize the provided CompanyId (if any)
                    if (!string.IsNullOrWhiteSpace(req.CompanyId))
                    {
                        if (!Guid.TryParse(req.CompanyId, out Guid newCompanyId))
                            return ApiResponseFactory.Error("Invalid Company ID format.",
                                StatusCodes.Status400BadRequest);

                        var companyExists = await db.Companies.AnyAsync(c => c.Id == newCompanyId);
                        if (!companyExists)
                            return ApiResponseFactory.Error("Company not found.", StatusCodes.Status400BadRequest);

                        // If the current user is a CustomerAdmin, ensure they are only assigning to their own companies
                        if (isCustomerAdmin && !customerAdminCompanyIds.Contains(newCompanyId))
                            return ApiResponseFactory.Error("You can only assign drivers to your associated companies.",
                                StatusCodes.Status403Forbidden);

                        // Update the Driver's CompanyId
                        driverEntity.CompanyId = newCompanyId;
                    }
                    else
                    {
                        // If CompanyId is not provided, set it to null (disassociate)
                        driverEntity.CompanyId = null;
                    }

                    // 11. Validate and update CarId (if provided)
                    if (!string.IsNullOrWhiteSpace(req.CarId))
                    {
                        if (!Guid.TryParse(req.CarId, out Guid newCarId))
                            return ApiResponseFactory.Error("Invalid Car ID format.",
                                StatusCodes.Status400BadRequest);

                        var car = await db.Cars.FirstOrDefaultAsync(c => c.Id == newCarId);
                        if (car == null)
                            return ApiResponseFactory.Error("Car not found.", StatusCodes.Status400BadRequest);

                        // If the current user is a CustomerAdmin, ensure the car belongs to their company
                        if (isCustomerAdmin && !customerAdminCompanyIds.Contains(car.CompanyId))
                            return ApiResponseFactory.Error("You can only assign cars from your associated companies.",
                                StatusCodes.Status403Forbidden);

                        // Check if the car is already assigned to another driver (excluding the current driver)
                        var existingDriverForCar = await db.Drivers
                            .FirstOrDefaultAsync(d => d.CarId == newCarId && d.Id != driverEntity.Id && !d.IsDeleted);
                        
                        if (existingDriverForCar != null)
                        {
                            // Unassign the car from the previous driver (allow reassignment)
                            existingDriverForCar.CarId = null;
                        }

                        // Update the Driver's CarId
                        driverEntity.CarId = newCarId;
                    }
                    else
                    {
                        // If CarId is not provided, set it to null (unassign car)
                        driverEntity.CarId = null;
                    }

                    // 12.5. Handle "Used by" companies update (null = don't update, empty = clear all, list = replace)
                    if (req.UsedByCompanyIds != null)
                    {
                        // Remove all existing associations
                        var existingUsages = await db.DriverUsedByCompanies
                            .Where(duc => duc.DriverId == driverEntity.Id)
                            .ToListAsync();
                        db.DriverUsedByCompanies.RemoveRange(existingUsages);
                        
                        // Add new associations
                        if (req.UsedByCompanyIds.Any())
                        {
                            foreach (var companyIdStr in req.UsedByCompanyIds)
                            {
                                if (Guid.TryParse(companyIdStr, out var usedByCompanyGuid))
                                {
                                    // Verify company exists
                                    var companyExists = await db.Companies.AnyAsync(c => c.Id == usedByCompanyGuid);
                                    if (companyExists)
                                    {
                                        // If customer admin, ensure they can only assign to their own companies
                                        if (isCustomerAdmin && !customerAdminCompanyIds.Contains(usedByCompanyGuid))
                                            continue; // Skip companies not managed by this admin
                                        
                                        db.DriverUsedByCompanies.Add(new DriverUsedByCompany
                                        {
                                            Id = Guid.NewGuid(),
                                            DriverId = driverEntity.Id,
                                            CompanyId = usedByCompanyGuid
                                        });
                                    }
                                }
                            }
                        }
                    }

                    // 13. Save changes to the database
                    await db.SaveChangesAsync();

                    // 14. Commit the transaction
                    await transaction.CommitAsync();

                    // 15. Retrieve updated driver information for the response
                    var updatedDriver = await db.Drivers
                        .Include(d => d.Company)
                        .Include(d => d.Car)
                        .Include(d => d.UsedByCompanies)
                            .ThenInclude(uc => uc.Company)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.AspNetUserId == targetUser.Id);

                    var driverInfo = updatedDriver == null
                        ? null
                        : new
                        {
                            DriverId = updatedDriver.Id,
                            CompanyId = updatedDriver.CompanyId,
                            CompanyName = updatedDriver.Company?.Name,
                            CarId = updatedDriver.CarId,
                            CarLicensePlate = updatedDriver.Car?.LicensePlate,
                            CarVehicleYear = updatedDriver.Car?.VehicleYear,
                            CarRegistrationDate = updatedDriver.Car?.RegistrationDate,
                            UsedByCompanies = updatedDriver.UsedByCompanies.Select(uc => new
                            {
                                Id = uc.Company.Id,
                                Name = uc.Company.Name
                            }).ToList()
                        };

                    // 16. Return success response
                    return ApiResponseFactory.Success(driverInfo, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    // 16. Rollback the transaction on error
                    await transaction.RollbackAsync();

                    // 17. Log the exception details to the console
                    Console.WriteLine($"[Error] An exception occurred while updating the driver.");
                    Console.WriteLine($"[Error] DriverId: {driverEntity.Id}");
                    Console.WriteLine($"[Error] TargetUserId: {targetUser.Id}");
                    Console.WriteLine($"[Error] Exception Message: {ex.Message}");
                    Console.WriteLine($"[Error] Stack Trace: {ex.StackTrace}");

                    // 18. Return a generic error response to the client
                    return ApiResponseFactory.Error(
                        "An error occurred while updating the driver's information.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        app.MapPut("/users/{id}/contact-person",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                [FromBody] UpdateContactPersonRequest req,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                // 1. Validate and parse the user ID from the route
                if (!Guid.TryParse(id, out Guid userGuid))
                    return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);

                // 2. Retrieve the target user by ID
                var targetUser = await userManager.FindByIdAsync(userGuid.ToString());
                if (targetUser == null)
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                // 3. Retrieve the current user's ID
                var currentUserId = userManager.GetUserId(currentUser);

                // 4. Determine the roles of the current user
                bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                // 5. If the current user is a "customerAdmin", retrieve their associated company IDs
                List<Guid> customerAdminCompanyIds = new List<Guid>();
                if (isCustomerAdmin)
                {
                    // Retrieve the ContactPerson entity associated with the current user
                    var currentContactPerson = await db.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson != null)
                    {
                        // Retrieve associated CompanyIds from ContactPersonClientCompany
                        var directCompanyIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .Distinct()
                            .ToListAsync();

                        // Retrieve associated ClientIds
                        var associatedClientIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.ClientId.HasValue)
                            .Select(cpc => cpc.ClientId.Value)
                            .Distinct()
                            .ToListAsync();

                        // Retrieve parent companies of associated clients
                        var parentCompanyIds = await db.Clients
                            .Where(cl => associatedClientIds.Contains(cl.Id))
                            .Select(cl => cl.CompanyId)
                            .Distinct()
                            .ToListAsync();

                        // Merge direct company IDs and parent company IDs from clients
                        customerAdminCompanyIds = directCompanyIds
                            .Concat(parentCompanyIds)
                            .Distinct()
                            .ToList();
                    }
                }

                // 6. Retrieve the existing ContactPerson entity for the target user
                var contactPerson = await db.ContactPersons
                    .FirstOrDefaultAsync(cp => cp.AspNetUserId == targetUser.Id);
                if (contactPerson == null)
                    return ApiResponseFactory.Error("Contact person not found for this user.",
                        StatusCodes.Status404NotFound);

                // 7. If the current user is a "customerAdmin", verify the contact person's current CompanyIds
                if (isCustomerAdmin)
                {
                    // Retrieve the contact person's current CompanyIds
                    var directCompanyIds = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.ContactPersonId == contactPerson.Id && cpc.CompanyId.HasValue)
                        .Select(cpc => cpc.CompanyId.Value)
                        .Distinct()
                        .ToListAsync();

                    // Retrieve associated ClientIds
                    var associatedClientIds = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.ContactPersonId == contactPerson.Id && cpc.ClientId.HasValue)
                        .Select(cpc => cpc.ClientId.Value)
                        .Distinct()
                        .ToListAsync();

                    // Retrieve parent companies of associated clients
                    var parentCompanyIds = await db.Clients
                        .Where(cl => associatedClientIds.Contains(cl.Id))
                        .Select(cl => cl.CompanyId)
                        .Distinct()
                        .ToListAsync();

                    // Merge direct company IDs and parent company IDs from clients
                    var contactPersonCompanyIds = directCompanyIds
                        .Concat(parentCompanyIds)
                        .Distinct()
                        .ToList();

                    // Check if there is any overlap between customerAdminCompanyIds and contactPersonCompanyIds
                    bool hasOverlap = contactPersonCompanyIds.Any(cpc => customerAdminCompanyIds.Contains(cpc));

                    if (!hasOverlap)
                        return ApiResponseFactory.Error("You are not authorized to modify this contact person.",
                            StatusCodes.Status403Forbidden);
                }

                // 8. Begin a database transaction to ensure atomicity
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // 9. Validate and authorize each CompanyId if provided
                    if (req.CompanyIds != null)
                    {
                        foreach (var compStr in req.CompanyIds)
                        {
                            if (!Guid.TryParse(compStr, out Guid compGuid))
                                return ApiResponseFactory.Error($"Invalid Company ID format: '{compStr}'.",
                                    StatusCodes.Status400BadRequest);

                            var companyExists = await db.Companies.AnyAsync(c => c.Id == compGuid);
                            if (!companyExists)
                                return ApiResponseFactory.Error($"Company with ID '{compGuid}' does not exist.",
                                    StatusCodes.Status400BadRequest);

                            // If the requester is a "customerAdmin", ensure they can only associate with their own companies
                            if (isCustomerAdmin && !customerAdminCompanyIds.Contains(compGuid))
                                return ApiResponseFactory.Error(
                                    $"You do not have permission to associate with Company ID '{compGuid}'.",
                                    StatusCodes.Status403Forbidden);
                        }
                    }

                    // 10. Validate each ClientId if provided
                    if (req.ClientIds != null)
                    {
                        foreach (var clientStr in req.ClientIds)
                        {
                            if (!Guid.TryParse(clientStr, out Guid clientGuid))
                                return ApiResponseFactory.Error($"Invalid Client ID format: '{clientStr}'.",
                                    StatusCodes.Status400BadRequest);

                            var clientExists = await db.Clients.AnyAsync(c => c.Id == clientGuid);
                            if (!clientExists)
                                return ApiResponseFactory.Error($"Client with ID '{clientGuid}' does not exist.",
                                    StatusCodes.Status400BadRequest);
                        }
                    }

                    // 11. Remove existing associations
                    var existingAssociations = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.ContactPersonId == contactPerson.Id)
                        .ToListAsync();
                    db.ContactPersonClientCompanies.RemoveRange(existingAssociations);

                    // 12. Add new associations based on provided CompanyIds and ClientIds
                    if (req.CompanyIds != null && req.CompanyIds.Any())
                    {
                        foreach (var compStr in req.CompanyIds)
                        {
                            var compGuid = Guid.Parse(compStr);
                            var newCpc = new ContactPersonClientCompany
                            {
                                Id = Guid.NewGuid(),
                                ContactPersonId = contactPerson.Id,
                                CompanyId = compGuid,
                                ClientId = null // Assuming nullable
                            };
                            db.ContactPersonClientCompanies.Add(newCpc);
                        }
                    }

                    if (req.ClientIds != null && req.ClientIds.Any())
                    {
                        foreach (var clientStr in req.ClientIds)
                        {
                            var clientGuid = Guid.Parse(clientStr);
                            var newCpc = new ContactPersonClientCompany
                            {
                                Id = Guid.NewGuid(),
                                ContactPersonId = contactPerson.Id,
                                CompanyId = null, // Assuming nullable
                                ClientId = clientGuid
                            };
                            db.ContactPersonClientCompanies.Add(newCpc);
                        }
                    }

                    // 13. (Future-Proofing) Update other ContactPerson properties here as needed
                    // Example:
                    // if (!string.IsNullOrWhiteSpace(req.PhoneNumber))
                    // {
                    //     contactPerson.PhoneNumber = req.PhoneNumber;
                    // }

                    // 14. Save changes to the database
                    await db.SaveChangesAsync();

                    // 15. Commit the transaction
                    await transaction.CommitAsync();

                    // 16. Retrieve updated associations for the response
                    var clientsCompanies = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.ContactPersonId == contactPerson.Id)
                        .Include(cpc => cpc.Company)
                        .Include(cpc => cpc.Client)
                        .AsNoTracking()
                        .Select(cpc => new
                        {
                            CompanyId = cpc.CompanyId,
                            CompanyName = cpc.Company != null ? cpc.Company.Name : null,
                            ClientId = cpc.ClientId,
                            ClientName = cpc.Client != null ? cpc.Client.Name : null
                        })
                        .ToListAsync();

                    // 17. Prepare the response payload
                    var contactPersonInfo = new
                    {
                        ContactPersonId = contactPerson.Id,
                        clientsCompanies = clientsCompanies
                        // Include additional properties here as needed
                    };

                    // 18. Return success response
                    return ApiResponseFactory.Success(contactPersonInfo, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    // 19. Rollback the transaction on error
                    await transaction.RollbackAsync();

                    // 20. Log the exception details to the console
                    Console.WriteLine($"[Error] An exception occurred while updating the contact person.");
                    Console.WriteLine($"[Error] ContactPersonId: {contactPerson.Id}");
                    Console.WriteLine($"[Error] TargetUserId: {targetUser.Id}");
                    Console.WriteLine($"[Error] Exception Message: {ex.Message}");
                    Console.WriteLine($"[Error] Stack Trace: {ex.StackTrace}");

                    // 21. Return a generic error response to the client
                    return ApiResponseFactory.Error(
                        "An error occurred while updating the contact person.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        app.MapDelete("/contactpersons/{contactPersonId:guid}",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string contactPersonId,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // 1️⃣ Validate GUID format
                    if (!Guid.TryParse(contactPersonId, out Guid contactPersonGuid))
                    {
                        return ApiResponseFactory.Error("Invalid contact person ID format. Must be a valid GUID.",
                            StatusCodes.Status400BadRequest);
                    }

                    // 2️⃣ Find the contact person
                    var contactPerson = await db.ContactPersons
                        .Include(cp => cp.ContactPersonClientCompanies)
                        .FirstOrDefaultAsync(cp => cp.Id == contactPersonGuid);

                    if (contactPerson == null)
                    {
                        return ApiResponseFactory.Error("Contact person not found.", StatusCodes.Status404NotFound);
                    }

                    // 3️⃣ Determine user roles
                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                    if (!isGlobalAdmin && !isCustomerAdmin)
                    {
                        return ApiResponseFactory.Error("Unauthorized to delete this contact person.",
                            StatusCodes.Status403Forbidden);
                    }

                    // 4️⃣ If the requester is a **Customer Admin**, check if they are associated with this contact person
                    if (!isGlobalAdmin)
                    {
                        var currentUserId = userManager.GetUserId(currentUser);

                        // Get the current user's ContactPerson record
                        var currentContactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                        if (currentContactPerson == null)
                        {
                            return ApiResponseFactory.Error("Your ContactPerson profile was not found.",
                                StatusCodes.Status403Forbidden);
                        }

                        // Get all **companies** that the current **customerAdmin** is assigned to
                        var adminCompanyIds = currentContactPerson.ContactPersonClientCompanies
                            .Where(cpc => cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId!.Value)
                            .Distinct()
                            .ToList();

                        // Get all **clients** owned by those companies
                        var clientsOwnedByAdminCompanies = await db.Clients
                            .Where(client => adminCompanyIds.Contains(client.CompanyId))
                            .Select(client => client.Id)
                            .Distinct()
                            .ToListAsync();

                        // Check if the contact person is assigned to **ANY** of the same companies or clients
                        bool isAuthorizedToDelete = contactPerson.ContactPersonClientCompanies
                            .Any(cpc =>
                                (cpc.CompanyId.HasValue && adminCompanyIds.Contains(cpc.CompanyId.Value)) ||
                                (cpc.ClientId.HasValue && clientsOwnedByAdminCompanies.Contains(cpc.ClientId.Value))
                            );

                        if (!isAuthorizedToDelete)
                        {
                            return ApiResponseFactory.Error(
                                "Unauthorized: You cannot delete this contact person as they are not associated with your company or clients.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // 5 Soft-delete the associated ApplicationUser instead of ContactPerson
                    var user = await db.Users.FindAsync(contactPerson.AspNetUserId);
                    if (user == null)
                    {
                        throw new InvalidOperationException(
                            $"User with ID {contactPerson.AspNetUserId} not found. Cannot delete contact person.");
                    }

                    user.IsDeleted = true;
                    contactPerson.IsDeleted = true;
                    await db.SaveChangesAsync();

                    // 6️⃣ Remove all ContactPersonClientCompanies associated with this contact person
                    var cpcRecords = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.ContactPersonId == contactPersonGuid)
                        .ToListAsync();

                    if (cpcRecords.Any())
                    {
                        db.ContactPersonClientCompanies.RemoveRange(cpcRecords);
                        await db.SaveChangesAsync();
                    }

                    // 7️⃣ Commit transaction
                    await transaction.CommitAsync();

                    return ApiResponseFactory.Success("Contact person deleted successfully.",
                        StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    // 8️⃣ Roll back on failure
                    await transaction.RollbackAsync();

                    Console.WriteLine($"[Error] {ex.Message}");
                    Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while deleting the contact person.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );

        app.MapDelete("/drivers/{id:guid}",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                Guid id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    using var transaction = await db.Database.BeginTransactionAsync();

                    // Validate driver ID
                    var driver = await db.Drivers.FindAsync(id);
                    if (driver == null)
                    {
                        return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                    }

                    // Fetch the associated ApplicationUser
                    var user = await db.Users.FindAsync(driver.AspNetUserId);
                    if (user == null)
                    {
                        throw new InvalidOperationException(
                            $"User with ID {driver.AspNetUserId} not found. Cannot delete driver.");
                    }

                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                    // If the requester is a customerAdmin, ensure they are associated with the driver's company
                    if (isCustomerAdmin)
                    {
                        var currentUserId = userManager.GetUserId(currentUser);
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                        if (contactPerson == null)
                        {
                            return ApiResponseFactory.Error("No ContactPerson profile found.",
                                StatusCodes.Status403Forbidden);
                        }

                        // Get companies the customer admin is associated with
                        var customerAdminCompanyIds = contactPerson.ContactPersonClientCompanies
                            .Where(cpc => cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .ToList();

                        if (!customerAdminCompanyIds.Contains(driver.CompanyId.Value))
                        {
                            return ApiResponseFactory.Error(
                                "Unauthorized: You cannot delete drivers from companies you are not associated with.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // Soft-delete the ApplicationUser
                    user.IsDeleted = true;
                    await db.SaveChangesAsync();

                    // Unassign car from driver (1-1 relationship)
                    if (driver.CarId.HasValue)
                    {
                        driver.CarId = null;
                        await db.SaveChangesAsync();
                    }

                    // Soft delete the driver by setting IsDeleted to true
                    driver.IsDeleted = true;
                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return ApiResponseFactory.Success("Driver deleted successfully.", StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] {ex.Message}");
                    Console.WriteLine($"[Stack] {ex.StackTrace}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while deleting the driver.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        // GET /customeradmins => Paginated list of customer admin users
        app.MapGet("/customeradmins",
            [Authorize(Roles = "globalAdmin")]
            async (
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 1000,
                [FromQuery] string? search = null
            ) =>
            {
                try
                {
                    // Get all users with customerAdmin role
                    var customerAdminRole = await db.Roles
                        .FirstOrDefaultAsync(r => r.Name == "customerAdmin");

                    if (customerAdminRole == null)
                    {
                        return ApiResponseFactory.Error(
                            "CustomerAdmin role not found in the system.",
                            StatusCodes.Status500InternalServerError
                        );
                    }

                    // Query users who have the customerAdmin role
                    var customerAdminUserIds = await db.UserRoles
                        .Where(ur => ur.RoleId == customerAdminRole.Id)
                        .Select(ur => ur.UserId)
                        .ToListAsync();

                    // Build the query for customer admin users
                    IQueryable<ApplicationUser> query = db.Users
                        .Where(u => customerAdminUserIds.Contains(u.Id))
                        .AsNoTracking();

                    // Apply search filter if provided
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var searchLower = search.ToLower();
                        query = query.Where(u =>
                            u.Email.ToLower().Contains(searchLower) ||
                            u.FirstName.ToLower().Contains(searchLower) ||
                            u.LastName.ToLower().Contains(searchLower)
                        );
                    }

                    // Get total count for pagination
                    var totalCount = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                    // Apply pagination and get results
                    var customerAdmins = await query
                        .OrderBy(u => u.Email)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(u => new
                        {
                            u.Id,
                            u.Email,
                            u.FirstName,
                            u.LastName,
                            u.Address,
                            u.PhoneNumber,
                            u.Postcode,
                            u.City,
                            u.Country,
                            u.Remark,
                            Roles = (from ur in db.UserRoles
                                join r in db.Roles on ur.RoleId equals r.Id
                                where ur.UserId == u.Id
                                select r.Name).ToList(),
                            
                            ContactPersonInfo = (from cp in db.ContactPersons
                                where cp.AspNetUserId == u.Id
                                select new
                                {
                                    ContactPersonId = cp.Id,
                                    AssociatedCompanies = (from cpc in db.ContactPersonClientCompanies
                                        join c in db.Companies on cpc.CompanyId equals c.Id
                                        where cpc.ContactPersonId == cp.Id && cpc.CompanyId.HasValue
                                        select new
                                        {
                                            c.Id,
                                            c.Name
                                        }).Distinct().ToList(),
                                    AssociatedClients = (from cpc in db.ContactPersonClientCompanies
                                        join cl in db.Clients on cpc.ClientId equals cl.Id
                                        where cpc.ContactPersonId == cp.Id && cpc.ClientId.HasValue
                                        select new
                                        {
                                            cl.Id,
                                            cl.Name
                                        }).Distinct().ToList()
                                }).FirstOrDefault()
                        })
                        .ToListAsync();

                    // Build response data
                    var responseData = new
                    {
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        Data = customerAdmins
                    };

                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] An error occurred while fetching customer admins.");
                    Console.WriteLine($"[Error] Exception Message: {ex.Message}");
                    Console.WriteLine($"[Error] Stack Trace: {ex.StackTrace}");

                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while fetching customer admins.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        return app;
    }
}