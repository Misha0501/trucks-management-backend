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
                            .Where(cpc => cpc.ContactPersonId == contactPerson.Id)
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


        app.MapPost("/users/change-password", async (
                ChangePasswordRequest req,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext
            ) =>
            {
                // 1) Get the user’s ID from JWT claims (e.g., "sub")
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
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                ApplicationDbContext db,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10
            ) =>
            {
                // 1) Total user count for pagination
                var totalUsers = await db.Users.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

                // 2) Query users with additional driver/contact person info
                var pagedUsers = await db.Users
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

                // 3) Build the response object with pagination info
                var responseData = new
                {
                    totalUsers,
                    totalPages,
                    pageNumber,
                    pageSize,
                    data = pagedUsers
                };

                // 4) Return success response
                return ApiResponseFactory.Success(
                    responseData,
                    StatusCodes.Status200OK
                );
            }
        );


        app.MapGet("/users/{id}",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager
            ) =>
            {
                // 1) Retrieve user by ID
                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return ApiResponseFactory.Error(
                        "User not found.",
                        StatusCodes.Status404NotFound
                    );
                }

                // 2) Retrieve user roles
                var roles = await userManager.GetRolesAsync(user);

                // 3) Determine role type
                bool isDriver = roles.Contains("driver");
                bool isContactPerson = !isDriver; // Binary assumption: if not driver, then contact person

                object? driverInfo = null;
                object? contactPersonInfo = null;

                // 4) Load driver information if applicable
                if (isDriver)
                {
                    driverInfo = await db.Drivers
                        .AsNoTracking()
                        .Where(d => d.AspNetUserId == user.Id)
                        .Include(d => d.Company)
                        .Select(d => new
                        {
                            DriverId = d.Id,
                            CompanyId = d.CompanyId,
                            CompanyName = d.Company != null ? d.Company.Name : null
                        })
                        .FirstOrDefaultAsync();
                }

                // 5) Load contact person information if applicable
                if (isContactPerson)
                {
                    var contactPerson = await db.ContactPersons
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == user.Id);

                    if (contactPerson != null)
                    {
                        var companiesAndClients = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == contactPerson.Id)
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

                // 6) Prepare response data
                var data = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Address,
                    user.PhoneNumber,
                    user.Postcode,
                    user.City,
                    user.Country,
                    user.Remark,
                    Roles = roles,
                    DriverInfo = driverInfo,
                    ContactPersonInfo = contactPersonInfo
                };

                // 7) Return standardized success response
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
                RoleManager<ApplicationRole> roleManager
            ) =>
            {
                // Validate and parse user ID
                if (!Guid.TryParse(id, out Guid userGuid))
                    return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);

                // Find user by ID
                var user = await userManager.FindByIdAsync(userGuid.ToString());
                if (user == null)
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                // Update basic fields
                if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != user.Email)
                {
                    var existing = await userManager.FindByEmailAsync(req.Email);
                    if (existing != null && existing.Id != user.Id)
                        return ApiResponseFactory.Error("Email already in use.", StatusCodes.Status400BadRequest);
                    user.Email = req.Email;
                }

                if (!string.IsNullOrWhiteSpace(req.FirstName)) user.FirstName = req.FirstName;
                if (!string.IsNullOrWhiteSpace(req.LastName)) user.LastName = req.LastName;
                if (!string.IsNullOrWhiteSpace(req.Address)) user.Address = req.Address;
                if (!string.IsNullOrWhiteSpace(req.PhoneNumber)) user.PhoneNumber = req.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(req.Postcode)) user.Postcode = req.Postcode;
                if (!string.IsNullOrWhiteSpace(req.City)) user.City = req.City;
                if (!string.IsNullOrWhiteSpace(req.Country)) user.Country = req.Country;
                if (!string.IsNullOrWhiteSpace(req.Remark)) user.Remark = req.Remark;

                // Handle roles update logic
                if (req.Roles != null)
                {
                    if (req.Roles.Count == 0)
                    {
                        var currentRoles = await userManager.GetRolesAsync(user);
                        if (currentRoles.Count > 0)
                        {
                            var removeAllResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
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

                        var currentRoles = await userManager.GetRolesAsync(user);
                        var rolesToAdd = req.Roles.Except(currentRoles).ToList();
                        var rolesToRemove = currentRoles.Except(req.Roles).ToList();

                        if (rolesToRemove.Count > 0)
                        {
                            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                            if (!removeResult.Succeeded)
                            {
                                var errors = removeResult.Errors.Select(e => e.Description).ToList();
                                return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                            }
                        }

                        if (rolesToAdd.Count > 0)
                        {
                            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                            if (!addResult.Succeeded)
                            {
                                var errors = addResult.Errors.Select(e => e.Description).ToList();
                                return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                            }
                        }
                    }
                }

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = updateResult.Errors.Select(e => e.Description).ToList();
                    return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
                }

                var updatedRoles = await userManager.GetRolesAsync(user);

                var updatedUserData = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Address,
                    user.PhoneNumber,
                    user.Postcode,
                    user.City,
                    user.Country,
                    user.Remark,
                    Roles = updatedRoles
                };

                return ApiResponseFactory.Success(updatedUserData, StatusCodes.Status200OK);
            });


        app.MapPut("/users/{id}/driver",
            [Authorize(Roles = "globalAdmin, customerAdmin, driver")]
            async (
                string id,
                [FromBody] UpdateDriverRequest req,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager
            ) =>
            {
                if (!Guid.TryParse(id, out Guid userGuid))
                    return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);

                var user = await userManager.FindByIdAsync(userGuid.ToString());
                if (user == null)
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                if (!string.IsNullOrWhiteSpace(req.CompanyId))
                {
                    if (!Guid.TryParse(req.CompanyId, out var newCompanyId))
                        return ApiResponseFactory.Error("Invalid Company ID format.", StatusCodes.Status400BadRequest);

                    var companyExists = await db.Companies.AnyAsync(c => c.Id == newCompanyId);
                    if (!companyExists)
                        return ApiResponseFactory.Error("Company not found.", StatusCodes.Status400BadRequest);

                    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == user.Id)
                                 ?? new Driver { Id = Guid.NewGuid(), AspNetUserId = user.Id };

                    driver.CompanyId = newCompanyId;
                    if (driver.Id == Guid.Empty) db.Drivers.Add(driver);
                }
                else
                {
                    var existingDriver = await db.Drivers.FirstOrDefaultAsync(d => d.AspNetUserId == user.Id);
                    if (existingDriver != null)
                        existingDriver.CompanyId = null;
                }

                await db.SaveChangesAsync();

                var updatedDriver = await db.Drivers
                    .Include(d => d.Company)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.AspNetUserId == user.Id);

                var driverInfo = updatedDriver == null
                    ? null
                    : new
                    {
                        DriverId = updatedDriver.Id,
                        CompanyId = updatedDriver.CompanyId,
                        CompanyName = updatedDriver.Company?.Name
                    };

                return ApiResponseFactory.Success(driverInfo, StatusCodes.Status200OK);
            });

        app.MapPut("/users/{id}/contact",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                [FromBody] UpdateContactPersonRequest req,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager
            ) =>
            {
                // 1. Validate and parse user ID
                if (!Guid.TryParse(id, out Guid userGuid))
                    return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);

                // 2. Find user by ID
                var user = await userManager.FindByIdAsync(userGuid.ToString());
                if (user == null)
                    return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);

                // 3. Retrieve existing ContactPerson entity
                var contactPerson = await db.ContactPersons
                    .FirstOrDefaultAsync(cp => cp.AspNetUserId == user.Id);
                if (contactPerson == null)
                    return ApiResponseFactory.Error("Contact person not found for this user.",
                        StatusCodes.Status404NotFound);

                // 4. Validate CompanyIds
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
                    }
                }

                // 5. Validate ClientIds
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

                // 6. Remove existing associations
                var existingAssociations = await db.ContactPersonClientCompanies
                    .Where(cpc => cpc.ContactPersonId == contactPerson.Id)
                    .ToListAsync();
                db.ContactPersonClientCompanies.RemoveRange(existingAssociations);

                // 7. Add new associations based on provided CompanyIds and ClientIds
                if (req.CompanyIds != null && req.CompanyIds.Any())
                {
                    // Only CompanyIds provided: associate contact person with companies (ClientId = null)
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
                    // Only ClientIds provided: associate contact person with clients (CompanyId = null)
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

                // 8. Save changes to the database
                var saveResult = await db.SaveChangesAsync();
                if (saveResult < 0)
                {
                    return ApiResponseFactory.Error(
                        "An error occurred while updating the contact person.",
                        StatusCodes.Status500InternalServerError
                    );
                }

                // 9. Retrieve updated associations
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

                // 10. Prepare ContactPersonInfo for response
                var contactPersonInfo = new
                {
                    ContactPersonId = contactPerson.Id,
                    clientsCompanies = clientsCompanies
                };

                // 11. Return success response
                return ApiResponseFactory.Success(contactPersonInfo, StatusCodes.Status200OK);
            });


        return app;
    }
}