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
                UserManager<ApplicationUser> userManager) =>
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

                // 4) Prepare the returned data
                var data = new
                {
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
                };
                // 5) Return a standardized success response
                return ApiResponseFactory.Success(
                    data: data,
                    statusCode: StatusCodes.Status200OK
                );
            })
            .RequireAuthorization(); // Only authenticated users

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
        // app.MapGet("/users",
        //     [Authorize(Roles = "globalAdmin, customerAdmin")]
        //     async (
        //         // Required parameters first
        //         ApplicationDbContext db,
        //         // Optional query parameters second
        //         [FromQuery] int pageNumber = 1,
        //         [FromQuery] int pageSize = 10
        //     ) =>
        //     {
        //         // 1) Total user count (for pagination metadata)
        //         var totalUsers = await db.Users.CountAsync();
        //         var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
        //
        //         // 2) Query the users with EF Core
        //         //    a) Include the Company so we can map `CompanyName`.
        //         //    b) Apply pagination via Skip/Take.
        //         //    c) For roles, do a join on AspNetUserRoles / AspNetRoles.
        //         var pagedUsers = await db.Users
        //             .AsNoTracking()
        //             .Include(u => u.Company) // Load the related Company
        //             .OrderBy(u => u.Email)
        //             .Skip((pageNumber - 1) * pageSize)
        //             .Take(pageSize)
        //             .Select(u => new
        //             {
        //                 u.Id,
        //                 u.Email,
        //                 u.FirstName,
        //                 u.LastName,
        //
        //                 // Return both CompanyId & CompanyName
        //                 CompanyId = u.CompanyId,
        //                 CompanyName = u.Company.Name,
        //
        //                 Roles = (from ur in db.UserRoles
        //                     join r in db.Roles on ur.RoleId equals r.Id
        //                     where ur.UserId == u.Id
        //                     select r.Name).ToList()
        //             })
        //             .ToListAsync();
        //
        //         // 3) Build a response object with pagination info
        //         var responseData = new
        //         {
        //             totalUsers,
        //             totalPages,
        //             pageNumber,
        //             pageSize,
        //             data = pagedUsers
        //         };
        //
        //         // 4) Return using your custom API response
        //         return ApiResponseFactory.Success(
        //             responseData,
        //             StatusCodes.Status200OK
        //         );
        //     }
        // );

        // app.MapGet("/users/{id}",
        //     [Authorize(Roles = "globalAdmin, customerAdmin")]
        //     async (
        //         String id,
        //         ApplicationDbContext db
        //     ) =>
        //     {
        //         // 1) Find the user by ID, include the Company, exclude sensitive fields
        //         var user = await db.Users
        //             .AsNoTracking()
        //             .Include(u => u.Company)
        //             .Where(u => u.Id == id)
        //             .Select(u => new
        //             {
        //                 u.Id,
        //                 u.Email,
        //                 u.FirstName,
        //                 u.LastName,
        //                 u.Address,
        //                 u.PhoneNumber,
        //                 u.Postcode,
        //                 u.City,
        //                 u.Country,
        //                 u.Remark,
        //                 CompanyId = u.CompanyId,
        //                 CompanyName = u.Company.Name,
        //                 // Roles: fetch each role's ID and name
        //                 Roles = (from ur in db.UserRoles
        //                         join r in db.Roles on ur.RoleId equals r.Id
        //                         where ur.UserId == u.Id
        //                         select new
        //                         {
        //                             roleId = r.Id,
        //                             roleName = r.Name
        //                         })
        //                     .ToList()
        //             })
        //             .FirstOrDefaultAsync();
        //
        //         // 2) If user not found, return 404
        //         if (user == null)
        //         {
        //             return ApiResponseFactory.Error(
        //                 "User not found.",
        //                 StatusCodes.Status404NotFound
        //             );
        //         }
        //
        //         // 3) Return the user data, excluding passwords
        //         return ApiResponseFactory.Success(
        //             user,
        //             StatusCodes.Status200OK
        //         );
        //     }
        // );

        // app.MapPut("/users/{id}",
        //     [Authorize(Roles = "globalAdmin, customerAdmin")]
        //     async (
        //         string id,
        //         [FromBody] UpdateUserRequest req,
        //         ApplicationDbContext db,
        //         UserManager<ApplicationUser> userManager,
        //         RoleManager<ApplicationRole> roleManager
        //     ) =>
        //     {
        //         // Parse user ID
        //         if (!Guid.TryParse(id, out Guid userGuid))
        //         {
        //             return ApiResponseFactory.Error("Invalid user ID format.", StatusCodes.Status400BadRequest);
        //         }
        //
        //         // Find user by ID
        //         var user = await userManager.FindByIdAsync(userGuid.ToString());
        //         if (user == null)
        //         {
        //             return ApiResponseFactory.Error("User not found.", StatusCodes.Status404NotFound);
        //         }
        //
        //         // Check if new email is provided and different from current
        //         if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != user.Email)
        //         {
        //             // Check if another user already has this email
        //             var existingUserWithEmail = await userManager.FindByEmailAsync(req.Email);
        //             if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
        //             {
        //                 return ApiResponseFactory.Error("The email address is already in use.",
        //                     StatusCodes.Status400BadRequest);
        //             }
        //         }
        //
        //         // Update basic fields if provided and valid
        //         if (!string.IsNullOrWhiteSpace(req.Email)) user.Email = req.Email;
        //         if (!string.IsNullOrWhiteSpace(req.FirstName)) user.FirstName = req.FirstName;
        //         if (!string.IsNullOrWhiteSpace(req.LastName)) user.LastName = req.LastName;
        //         if (!string.IsNullOrWhiteSpace(req.Address)) user.Address = req.Address;
        //         if (!string.IsNullOrWhiteSpace(req.PhoneNumber)) user.PhoneNumber = req.PhoneNumber;
        //         if (!string.IsNullOrWhiteSpace(req.Postcode)) user.Postcode = req.Postcode;
        //         if (!string.IsNullOrWhiteSpace(req.City)) user.City = req.City;
        //         if (!string.IsNullOrWhiteSpace(req.Country)) user.Country = req.Country;
        //         if (!string.IsNullOrWhiteSpace(req.Remark)) user.Remark = req.Remark;
        //
        //         // Update company if provided
        //         if (!string.IsNullOrWhiteSpace(req.CompanyId))
        //         {
        //             if (!Guid.TryParse(req.CompanyId, out var newCompanyId))
        //             {
        //                 return ApiResponseFactory.Error("Invalid Company ID format.", StatusCodes.Status400BadRequest);
        //             }
        //
        //             var companyExists = await db.Companies.AnyAsync(c => c.Id == newCompanyId);
        //             if (!companyExists)
        //             {
        //                 return ApiResponseFactory.Error("Company not found.", StatusCodes.Status400BadRequest);
        //             }
        //
        //             user.CompanyId = newCompanyId;
        //         }
        //
        //         // Update user roles if provided
        //         if (req.Roles != null)
        //         {
        //             // If the roles array is empty, remove all roles from the user.
        //             if (req.Roles.Count == 0)
        //             {
        //                 var currentRoles = await userManager.GetRolesAsync(user);
        //                 if (currentRoles.Count > 0)
        //                 {
        //                     var removeAllResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        //                     if (!removeAllResult.Succeeded)
        //                     {
        //                         var errors = removeAllResult.Errors.Select(e => e.Description).ToList();
        //                         return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
        //                     }
        //                 }
        //             }
        //             // If roles array has items, update roles accordingly.
        //             else
        //             {
        //                 // Validate each role exists
        //                 foreach (var role in req.Roles)
        //                 {
        //                     if (!await roleManager.RoleExistsAsync(role))
        //                     {
        //                         return ApiResponseFactory.Error($"Role '{role}' does not exist.",
        //                             StatusCodes.Status400BadRequest);
        //                     }
        //                 }
        //
        //                 // Get current roles and compute differences
        //                 var currentRoles = await userManager.GetRolesAsync(user);
        //                 var rolesToAdd = req.Roles.Except(currentRoles).ToList();
        //                 var rolesToRemove = currentRoles.Except(req.Roles).ToList();
        //
        //                 if (rolesToRemove.Count > 0)
        //                 {
        //                     var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
        //                     if (!removeResult.Succeeded)
        //                     {
        //                         var errors = removeResult.Errors.Select(e => e.Description).ToList();
        //                         return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
        //                     }
        //                 }
        //
        //                 if (rolesToAdd.Count > 0)
        //                 {
        //                     var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
        //                     if (!addResult.Succeeded)
        //                     {
        //                         var errors = addResult.Errors.Select(e => e.Description).ToList();
        //                         return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
        //                     }
        //                 }
        //             }
        //         }
        //
        //         // Save user updates
        //         var updateResult = await userManager.UpdateAsync(user);
        //         if (!updateResult.Succeeded)
        //         {
        //             var errors = updateResult.Errors.Select(e => e.Description).ToList();
        //             return ApiResponseFactory.Error(errors, StatusCodes.Status400BadRequest);
        //         }
        //
        //         // Retrieve updated company name and roles for the response
        //         var company = await db.Companies.FindAsync(user.CompanyId);
        //         var updatedRoles = await userManager.GetRolesAsync(user);
        //
        //         var updatedUserData = new
        //         {
        //             user.Id,
        //             user.Email,
        //             user.FirstName,
        //             user.LastName,
        //             user.Address,
        //             user.PhoneNumber,
        //             user.Postcode,
        //             user.City,
        //             user.Country,
        //             user.Remark,
        //             user.CompanyId,
        //             CompanyName = company?.Name,
        //             Roles = updatedRoles
        //         };
        //
        //         return ApiResponseFactory.Success(updatedUserData, StatusCodes.Status200OK);
        //     });

        return app;
    }
}