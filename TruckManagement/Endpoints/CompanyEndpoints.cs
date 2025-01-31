using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints;

public static class CompanyEndpoints
{
    public static WebApplication MapCompanyEndpoints(this WebApplication app)
    {
        app.MapGet("/companies",
            [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
            async (
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 100
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

                // 3. If the user is not a globalAdmin or a ContactPerson, deny access
                if (!isGlobalAdmin && !isContactPerson)
                    return ApiResponseFactory.Error("Unauthorized to view companies.", StatusCodes.Status403Forbidden);

                // 4. If the user is a ContactPerson, retrieve their associated company and client-based company IDs
                List<Guid> contactPersonCompanyIds = new List<Guid>();

                if (isContactPerson)
                {
                    var currentContactPerson = await db.ContactPersons
                        .Include(cp => cp.ContactPersonClientCompanies)
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson != null)
                    {
                        // Retrieve companies directly associated with the contact person
                        var directCompanyIds = currentContactPerson.ContactPersonClientCompanies
                            .Where(cpc => cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value);

                        // Retrieve companies indirectly associated via clients
                        var clientBasedCompanyIds = currentContactPerson.ContactPersonClientCompanies
                            .Where(cpc => cpc.ClientId.HasValue)
                            .SelectMany(cpc => db.Clients
                                .Where(client => client.Id == cpc.ClientId)
                                .Select(client => client.CompanyId))
                            .Distinct();

                        // Combine both direct and indirect associations
                        contactPersonCompanyIds = directCompanyIds.Concat(clientBasedCompanyIds).Distinct().ToList();
                    }
                    else
                    {
                        return ApiResponseFactory.Error("ContactPerson profile not found.",
                            StatusCodes.Status403Forbidden);
                    }
                }

                // 5. Build the base query based on the user's role
                IQueryable<Company> companiesQuery = db.Companies.AsQueryable();

                if (isContactPerson)
                {
                    // Restrict to companies associated with the ContactPerson directly or via clients
                    companiesQuery = companiesQuery.Where(c => contactPersonCompanyIds.Contains(c.Id));
                }
                // If globalAdmin, no additional filtering is needed

                // 6. Get total company count after filtering for pagination
                var totalCompanies = await companiesQuery.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCompanies / pageSize);

                // 7. Apply pagination and select necessary fields
                var pagedCompanies = await companiesQuery
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name
                    })
                    .ToListAsync();

                // 8. Build the response object with pagination info
                var responseData = new
                {
                    totalCompanies,
                    totalPages,
                    pageNumber,
                    pageSize,
                    data = pagedCompanies
                };

                // 9. Return success response
                return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
            }
        );


        // 2) GET /companies/{id:guid} -> Single company, including its users
        app.MapGet("/companies/{id:guid}",
            [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
            async (
                Guid id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
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

                // 3. If the user is not a globalAdmin or a ContactPerson, deny access
                if (!isGlobalAdmin && !isContactPerson)
                    return ApiResponseFactory.Error("Unauthorized to view this company.",
                        StatusCodes.Status403Forbidden);

                // 4. If the user is a ContactPerson, verify association with the company
                if (isContactPerson)
                {
                    var currentContactPerson = await db.ContactPersons
                        .Include(cp => cp.ContactPersonClientCompanies)
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson == null)
                        return ApiResponseFactory.Error("ContactPerson profile not found.",
                            StatusCodes.Status403Forbidden);

                    // Direct association check
                    bool isDirectlyAssociated = currentContactPerson.ContactPersonClientCompanies
                        .Any(cpc => cpc.CompanyId == id);

                    // Indirect association via clients
                    bool isIndirectlyAssociated = currentContactPerson.ContactPersonClientCompanies
                        .Where(cpc => cpc.ClientId.HasValue)
                        .SelectMany(cpc => db.Clients
                            .Where(client => client.Id == cpc.ClientId)
                            .Select(client => client.CompanyId))
                        .Distinct()
                        .Any(companyId => companyId == id);

                    if (!isDirectlyAssociated && !isIndirectlyAssociated)
                        return ApiResponseFactory.Error("You are not authorized to view this company.",
                            StatusCodes.Status403Forbidden);
                }

                // 5. Retrieve the company with related data
                var company = await db.Companies
                    .AsNoTracking()
                    .Include(c => c.Drivers)
                    .ThenInclude(d => d.User)
                    .Include(c => c.ContactPersonClientCompanies)
                    .ThenInclude(cpc => cpc.ContactPerson)
                    .ThenInclude(cp => cp.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                    return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);

                // 6. Prepare Drivers data
                var drivers = company.Drivers.Select(d => new
                {
                    d.Id,
                    d.AspNetUserId,
                    User = new
                    {
                        d.User.Id,
                        d.User.Email,
                        d.User.FirstName,
                        d.User.LastName
                    }
                }).ToList();

                // 7. Prepare ContactPersons data
                var contactPersons = company.ContactPersonClientCompanies
                    .Select(cpc => new
                    {
                        ContactPersonId = cpc.ContactPerson.Id,
                        User = new
                        {
                            cpc.ContactPerson.User.Id,
                            cpc.ContactPerson.User.Email,
                            cpc.ContactPerson.User.FirstName,
                            cpc.ContactPerson.User.LastName
                        }
                    })
                    .Distinct()
                    .ToList();

                // 8. Prepare response data
                var data = new
                {
                    company.Id,
                    company.Name,
                    Drivers = drivers,
                    ContactPersons = contactPersons
                };

                // 9. Return success response
                return ApiResponseFactory.Success(data, StatusCodes.Status200OK);
            }
        );


        // 3) POST /companies -> Create a new company (Require globalAdmin)
        app.MapPost("/companies",
            [Authorize(Roles = "globalAdmin")] async (
                [FromBody] Company newCompany,
                ApplicationDbContext db) =>
            {
                if (newCompany.Id == Guid.Empty)
                    newCompany.Id = Guid.NewGuid();

                db.Companies.Add(newCompany);
                await db.SaveChangesAsync();

                return ApiResponseFactory.Success(newCompany, StatusCodes.Status201Created);
            });


        app.MapPut("/companies/{id:guid}",
            [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
            async (
                Guid id,
                ClaimsPrincipal user,
                [FromBody] Company updatedCompany,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager
            ) =>
            {
                // Retrieve the requesting user's ID
                var currentUserId = userManager.GetUserId(user);
                var roles = await userManager.GetRolesAsync(await userManager.FindByIdAsync(currentUserId));

                // Check if the user is a global admin
                bool isGlobalAdmin = roles.Contains("globalAdmin");

                // Retrieve the target company
                var existing = await db.Companies
                    .Include(c => c.ContactPersonClientCompanies)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (existing == null)
                {
                    return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
                }

                // Allow global admins to update the company
                if (isGlobalAdmin)
                {
                    existing.Name = updatedCompany.Name;
                    await db.SaveChangesAsync();
                    return ApiResponseFactory.Success(new
                    {
                        existing.Id,
                        existing.Name
                    });
                }

                // For non-global admins, check if they are an associated contact person
                var contactPerson = await db.ContactPersons
                    .Where(cp => cp.AspNetUserId == currentUserId)
                    .Select(cp => new
                    {
                        cp.Id,
                        CompanyIds = db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == cp.Id && cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (contactPerson == null || !contactPerson.CompanyIds.Contains(id))
                {
                    return ApiResponseFactory.Error("Unauthorized to update this company.",
                        StatusCodes.Status403Forbidden);
                }

                // Proceed with updating the company
                existing.Name = updatedCompany.Name;
                await db.SaveChangesAsync();
                return ApiResponseFactory.Success(new
                {
                    existing.Id,
                    existing.Name
                });
            });


        // 5) DELETE /companies/{id:guid} -> Delete (Require globalAdmin)
        app.MapDelete("/companies/{id:guid}", async (
                Guid id,
                ApplicationDbContext db) =>
            {
                var existing = await db.Companies.FindAsync(id);
                if (existing == null)
                {
                    return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
                }

                db.Companies.Remove(existing);
                await db.SaveChangesAsync();

                return ApiResponseFactory.Success("Company deleted successfully.", StatusCodes.Status200OK);
            })
            .RequireAuthorization("GlobalAdminOnly"); // <--- policy name

        return app;
    }
}