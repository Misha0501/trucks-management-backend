using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class DriversEndpoints
    {
        public static void MapDriversEndpoints(this WebApplication app)
        {
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
                            .Include(d => d.User)
                            .OrderBy(d => d.User.Email)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(d => new
                            {
                                d.Id,
                                d.CompanyId,
                                CompanyName = d.Company != null ? d.Company.Name : null,
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
                });
        }
    }
}