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

                // 4. If the user is a ContactPerson, retrieve their associated company IDs
                List<Guid> contactPersonCompanyIds = new List<Guid>();
                if (isContactPerson)
                {
                    // Retrieve the ContactPerson entity associated with the current user
                    var currentContactPerson = await db.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson != null)
                    {
                        // Retrieve associated CompanyIds from ContactPersonClientCompany
                        contactPersonCompanyIds = await db.ContactPersonClientCompanies
                            .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId.HasValue)
                            .Select(cpc => cpc.CompanyId.Value)
                            .ToListAsync();
                    }
                    else
                    {
                        // If the current user is a ContactPerson but has no associated ContactPerson record
                        return ApiResponseFactory.Error("ContactPerson profile not found.",
                            StatusCodes.Status403Forbidden);
                    }
                }

                // 5. Build the base query based on the user's role
                IQueryable<Company> companiesQuery = db.Companies.AsQueryable();

                if (isContactPerson)
                {
                    // Restrict to companies associated with the ContactPerson
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
                return ApiResponseFactory.Success(
                    responseData,
                    StatusCodes.Status200OK
                );
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
                    return ApiResponseFactory.Error("Unauthorized to view companies.", StatusCodes.Status403Forbidden);

                // 4. If the user is a ContactPerson, verify association with the company
                if (isContactPerson)
                {
                    // Retrieve the ContactPerson entity associated with the current user
                    var currentContactPerson = await db.ContactPersons
                        .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                    if (currentContactPerson == null)
                        return ApiResponseFactory.Error("ContactPerson profile not found.",
                            StatusCodes.Status403Forbidden);

                    // Check if the company is associated with the ContactPerson
                    bool isAssociated = await db.ContactPersonClientCompanies
                        .AnyAsync(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId == id);

                    if (!isAssociated)
                        return ApiResponseFactory.Error("You are not authorized to view this company.",
                            StatusCodes.Status403Forbidden);
                }

                // 5. Retrieve the company
                var company = await db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company == null)
                    return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);

                // 6. Retrieve Drivers associated with the company
                var drivers = await db.Drivers
                    .AsNoTracking()
                    .Where(d => d.CompanyId == id)
                    .Join(db.Users,
                        d => d.AspNetUserId,
                        u => u.Id,
                        (d, u) => new
                        {
                            DriverId = d.Id,
                            AspNetUserId = d.AspNetUserId,
                            User = new
                            {
                                u.Id,
                                u.Email,
                                u.FirstName,
                                u.LastName
                            }
                        })
                    .ToListAsync();

                // 7. Retrieve ContactPersons associated with the company via ContactPersonClientCompany
                var contactPersonIds = await db.ContactPersonClientCompanies
                    .AsNoTracking()
                    .Where(cpc => cpc.CompanyId == id)
                    .Select(cpc => cpc.ContactPersonId)
                    .Distinct()
                    .ToListAsync();

                var contactPersons = await db.ContactPersons
                    .AsNoTracking()
                    .Where(cp => contactPersonIds.Contains(cp.Id))
                    .Select(cp => new
                    {
                        ContactPersonId = cp.Id,
                        User = new
                        {
                            cp.User.Id,
                            cp.User.Email,
                            cp.User.FirstName,
                            cp.User.LastName
                        }
                    })
                    .ToListAsync();

                // 8. Prepare response data
                var data = new
                {
                    company.Id,
                    company.Name,
                    Drivers = drivers.Select(d => new
                    {
                        d.DriverId,
                        d.AspNetUserId,
                        d.User.Id,
                        d.User.Email,
                        d.User.FirstName,
                        d.User.LastName
                    }).ToList(),
                    ContactPersons = contactPersons.Select(cp => new
                    {
                        cp.ContactPersonId,
                        cp.User.Id,
                        cp.User.Email,
                        cp.User.FirstName,
                        cp.User.LastName
                    }).ToList()
                };

                // 9. Return success response
                return ApiResponseFactory.Success(data, StatusCodes.Status200OK);
            }
        );


        // // 3) POST /companies -> Create a new company (Require globalAdmin)
        // app.MapPost("/companies", async (
        //         [FromBody] Company newCompany,
        //         ApplicationDbContext db) =>
        //     {
        //         if (newCompany.Id == Guid.Empty)
        //             newCompany.Id = Guid.NewGuid();
        //
        //         db.Companies.Add(newCompany);
        //         await db.SaveChangesAsync();
        //
        //         return ApiResponseFactory.Success(newCompany, StatusCodes.Status201Created);
        //     })
        //     .RequireAuthorization("GlobalAdminOnly"); // <--- policy name
        //
        // // 4) PUT /companies/{id:guid} -> Update (Require globalAdmin)
        // app.MapPut("/companies/{id:guid}", async (
        //         Guid id,
        //         [FromBody] Company updatedCompany,
        //         ApplicationDbContext db) =>
        //     {
        //         var existing = await db.Companies.FindAsync(id);
        //         if (existing == null)
        //         {
        //             return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
        //         }
        //
        //         existing.Name = updatedCompany.Name;
        //         await db.SaveChangesAsync();
        //         return ApiResponseFactory.Success(existing);
        //     })
        //     .RequireAuthorization("GlobalAdminOnly"); // <--- policy name
        //
        // // 5) DELETE /companies/{id:guid} -> Delete (Require globalAdmin)
        // app.MapDelete("/companies/{id:guid}", async (
        //         Guid id,
        //         ApplicationDbContext db) =>
        //     {
        //         var existing = await db.Companies.FindAsync(id);
        //         if (existing == null)
        //         {
        //             return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
        //         }
        //
        //         db.Companies.Remove(existing);
        //         await db.SaveChangesAsync();
        //
        //         return ApiResponseFactory.Success("Company deleted successfully.", StatusCodes.Status200OK);
        //     })
        //     .RequireAuthorization("GlobalAdminOnly"); // <--- policy name

        return app;
    }
}