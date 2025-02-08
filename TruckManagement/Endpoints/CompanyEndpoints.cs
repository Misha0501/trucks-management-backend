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
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                HttpContext httpContext,
                [FromBody] Company newCompany,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager
            ) =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // 1️⃣ Identify user creating the company
                    var userEmail = httpContext.User.FindFirstValue(ClaimTypes.Email);
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        return ApiResponseFactory.Error("User email claim not found.",
                            StatusCodes.Status401Unauthorized);
                    }

                    var user = await userManager.FindByEmailAsync(userEmail);
                    if (user == null)
                    {
                        return ApiResponseFactory.Error("User not found.", StatusCodes.Status401Unauthorized);
                    }

                    // 2️⃣ Check the user's roles
                    var roles = await userManager.GetRolesAsync(user);
                    bool isGlobalAdmin = roles.Contains("globalAdmin");
                    bool isCustomerAdmin = roles.Contains("customerAdmin");

                    // 3️⃣ Ensure CustomerAdmin has an existing ContactPerson record
                    ContactPerson? contactPerson = null;
                    if (isCustomerAdmin)
                    {
                        contactPerson = await db.ContactPersons.FirstOrDefaultAsync(cp => cp.AspNetUserId == user.Id);
                        if (contactPerson == null)
                        {
                            return ApiResponseFactory.Error(
                                "CustomerAdmin must have an existing ContactPerson record before creating a company.",
                                StatusCodes.Status400BadRequest
                            );
                        }
                    }

                    // 4️⃣ Create the new company
                    newCompany.Id = Guid.NewGuid();
                    newCompany.IsApproved = isGlobalAdmin; // Only global admins can auto-approve

                    db.Companies.Add(newCompany);
                    await db.SaveChangesAsync();

                    // 5️⃣ If CustomerAdmin, link to new company
                    if (isCustomerAdmin && contactPerson != null)
                    {
                        var cpc = new ContactPersonClientCompany
                        {
                            Id = Guid.NewGuid(),
                            ContactPersonId = contactPerson.Id,
                            CompanyId = newCompany.Id,
                            ClientId = null // No client assigned yet
                        };

                        db.ContactPersonClientCompanies.Add(cpc);
                        await db.SaveChangesAsync();
                    }

                    // 6️⃣ Commit transaction & return filtered response
                    await transaction.CommitAsync();

                    // Return a **DTO** (to avoid infinite recursion)
                    var response = new
                    {
                        newCompany.Id,
                        newCompany.Name,
                        newCompany.IsApproved
                    };

                    return ApiResponseFactory.Success(response, StatusCodes.Status201Created);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    // Log error (consider using ILogger)
                    Console.Error.WriteLine($"Error while creating company: {ex.Message}");

                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while processing the request.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );


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
        app.MapDelete("/companies/{id:guid}",
            [Authorize(Roles = "globalAdmin, customerAdmin")]
            async (
                string id,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager,
                ClaimsPrincipal currentUser
            ) =>
            {
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // 1) Validate GUID format
                    if (!Guid.TryParse(id, out Guid companyGuid))
                    {
                        return ApiResponseFactory.Error("Invalid company ID format. Must be a valid GUID.",
                            StatusCodes.Status400BadRequest);
                    }

                    // 2) Find the company
                    var existingCompany = await db.Companies.FindAsync(companyGuid);
                    if (existingCompany == null)
                    {
                        return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
                    }

                    // 3) Determine if user is globalAdmin or customerAdmin
                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                    if (!isGlobalAdmin && !isCustomerAdmin)
                    {
                        return ApiResponseFactory.Error("Unauthorized to delete this company.",
                            StatusCodes.Status403Forbidden);
                    }

                    // 4) If customerAdmin, verify they are assigned to this company
                    if (isCustomerAdmin)
                    {
                        var currentUserId = userManager.GetUserId(currentUser);

                        // Retrieve the ContactPerson entity associated with the user
                        var currentContactPerson = await db.ContactPersons
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                        if (currentContactPerson == null)
                        {
                            return ApiResponseFactory.Error("ContactPerson profile not found.",
                                StatusCodes.Status403Forbidden);
                        }

                        // Check if user is assigned to this company
                        bool isAssigned = await db.ContactPersonClientCompanies
                            .AnyAsync(cpc =>
                                cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId == companyGuid);

                        if (!isAssigned)
                        {
                            return ApiResponseFactory.Error(
                                "You are not authorized to delete this company.",
                                StatusCodes.Status403Forbidden
                            );
                        }
                    }

                    // 5) Soft-delete the company
                    existingCompany.IsDeleted = true;
                    await db.SaveChangesAsync();

                    // 6) Remove all ContactPersonClientCompanies records related to this company
                    var cpcRecords = await db.ContactPersonClientCompanies
                        .Where(cpc => cpc.CompanyId == companyGuid)
                        .ToListAsync();

                    if (cpcRecords.Any())
                    {
                        db.ContactPersonClientCompanies.RemoveRange(cpcRecords);
                        await db.SaveChangesAsync();
                    }

                    // 7) Commit the transaction
                    await transaction.CommitAsync();

                    return ApiResponseFactory.Success("Company deleted successfully.", StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    Console.WriteLine($"[Error] {ex.Message}");
                    Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while deleting the company.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            }
        );

        return app;
    }
}