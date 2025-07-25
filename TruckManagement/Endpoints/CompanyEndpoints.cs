using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
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
                [FromQuery] int pageSize = 100,
                [FromQuery] string? search = null
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

                // Optional name search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    // Case-insensitive contains; use ILIKE for PostgreSQL
                    companiesQuery = companiesQuery.Where(c =>
                        EF.Functions.ILike(c.Name, $"%{search.Trim()}%"));
                }

                // 6. Get total company count after filtering for pagination
                var totalCompanies = await companiesQuery.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCompanies / pageSize);

                // 7. Apply pagination and select necessary fields with drivers
                var pagedCompanies = await companiesQuery
                    .AsNoTracking()
                    .Include(c => c.Drivers)
                        .ThenInclude(d => d.User)
                    .OrderBy(c => c.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CompanyDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Address = c.Address,
                        Postcode = c.Postcode,
                        City = c.City,
                        Country = c.Country,
                        PhoneNumber = c.PhoneNumber,
                        Email = c.Email,
                        Remark = c.Remark,
                        IsApproved = c.IsApproved,
                        Drivers = c.Drivers.Select(d => new DriverDto
                        {
                            DriverId = d.Id,
                            FirstName = d.User.FirstName,
                            LastName = d.User.LastName
                        }).ToList()
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
                var companyQuery = isGlobalAdmin
                    ? db.Companies.IgnoreQueryFilters()
                    : db.Companies;

                companyQuery = companyQuery
                    .Include(c => c.Drivers)
                    .ThenInclude(d => d.User)
                    .Include(c => c.ContactPersonClientCompanies)
                    .ThenInclude(cpc => cpc.ContactPerson)
                    .ThenInclude(cp => cp.User);

                var company = await companyQuery.FirstOrDefaultAsync(c => c.Id == id);
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
                    company.Address,
                    company.Postcode,
                    company.City,
                    company.Country,
                    company.PhoneNumber,
                    company.Email,
                    company.Remark,
                    company.IsApproved,
                    company.IsDeleted,
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
                [FromBody] CreateCompanyRequest request,
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
                    var newCompany = new Company
                    {
                        Id = Guid.NewGuid(),
                        Name = request.Name,
                        Address = request.Address,
                        Postcode = request.Postcode,
                        City = request.City,
                        Country = request.Country,
                        PhoneNumber = request.PhoneNumber,
                        Email = request.Email,
                        Remark = request.Remark,
                        IsApproved = isGlobalAdmin || isCustomerAdmin // Both get auto-approval
                    };

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

                    // Return a **CompanyDto** (to avoid infinite recursion)
                    var response = new CompanyDto
                    {
                        Id = newCompany.Id,
                        Name = newCompany.Name,
                        Address = newCompany.Address,
                        Postcode = newCompany.Postcode,
                        City = newCompany.City,
                        Country = newCompany.Country,
                        PhoneNumber = newCompany.PhoneNumber,
                        Email = newCompany.Email,
                        Remark = newCompany.Remark,
                        IsApproved = newCompany.IsApproved,
                        Drivers = new List<DriverDto>() // New company has no drivers yet
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
                [FromBody] UpdateCompanyRequest request,
                ApplicationDbContext db,
                UserManager<ApplicationUser> userManager
            ) =>
            {
                try
                {
                // ✅ Validate request
                if (request == null)
                {
                    return ApiResponseFactory.Error("Request body is required.", 
                        StatusCodes.Status400BadRequest);
                }
                
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return ApiResponseFactory.Error("Company name is required.", 
                        StatusCodes.Status400BadRequest);
                }

                // Retrieve the requesting user's ID  
                var currentUserId = userManager.GetUserId(user);

                // Check if the user is a global admin (using ClaimsPrincipal directly like other endpoints)
                bool isGlobalAdmin = user.IsInRole("globalAdmin");

                // Retrieve the target company
                var existing = await db.Companies
                    .IgnoreQueryFilters()
                    .Include(c => c.ContactPersonClientCompanies)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (existing == null)
                {
                    return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
                }

                // For non-global admins, check if they are an associated contact person
                if (!isGlobalAdmin)
                {
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
                }

                // Update all company fields
                existing.Name = request.Name;
                existing.Address = request.Address;
                existing.Postcode = request.Postcode;
                existing.City = request.City;
                existing.Country = request.Country;
                existing.PhoneNumber = request.PhoneNumber;
                existing.Email = request.Email;
                existing.Remark = request.Remark;

                await db.SaveChangesAsync();

                // Reload company with drivers for response
                var updatedCompany = await db.Companies
                    .Include(c => c.Drivers)
                        .ThenInclude(d => d.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                // ✅ Add null check for safety
                if (updatedCompany == null)
                {
                    return ApiResponseFactory.Error("Company not found after update.", StatusCodes.Status404NotFound);
                }

                // Return updated company as DTO
                var response = new CompanyDto
                {
                    Id = updatedCompany.Id,
                    Name = updatedCompany.Name,
                    Address = updatedCompany.Address,
                    Postcode = updatedCompany.Postcode,
                    City = updatedCompany.City,
                    Country = updatedCompany.Country,
                    PhoneNumber = updatedCompany.PhoneNumber,
                    Email = updatedCompany.Email,
                    Remark = updatedCompany.Remark,
                    IsApproved = updatedCompany.IsApproved,
                    Drivers = updatedCompany.Drivers
                        .Where(d => d.User != null) // ✅ Filter out drivers with null users
                        .Select(d => new DriverDto
                        {
                            DriverId = d.Id,
                            FirstName = d.User.FirstName,
                            LastName = d.User.LastName
                        }).ToList()
                };

                return ApiResponseFactory.Success(response);
                }
                catch (Exception ex)
                {
                    // Log the exception details for debugging
                    Console.WriteLine($"Error updating company {id}: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    return ApiResponseFactory.Error("An error occurred while updating the company.", 
                        StatusCodes.Status500InternalServerError);
                }
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

                    // 2) Determine if user is globalAdmin or customerAdmin
                    bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                    bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                    if (!isGlobalAdmin && !isCustomerAdmin)
                    {
                        return ApiResponseFactory.Error("Unauthorized to delete this company.",
                            StatusCodes.Status403Forbidden);
                    }

                    // 3) Find the company
                    var companyQuery = db.Companies.AsQueryable();

                    // ✅ If global admin, ignore query filters
                    if (isGlobalAdmin)
                    {
                        companyQuery = companyQuery.IgnoreQueryFilters();
                    }

                    // Fetch the company by ID
                    var existingCompany = await companyQuery.FirstOrDefaultAsync(c => c.Id == companyGuid);

                    if (existingCompany == null)
                    {
                        return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
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

        app.MapPut("/companies/{id}/approve",
            [Authorize(Roles = "globalAdmin")] async (
                string id,
                ApplicationDbContext db
            ) =>
            {
                // Validate ID format
                if (!Guid.TryParse(id, out Guid companyGuid))
                {
                    return ApiResponseFactory.Error("Invalid company ID format.", StatusCodes.Status400BadRequest);
                }

                await using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    // Bypass global query filters to include unapproved companies
                    var company = await db.Companies
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(c => c.Id == companyGuid);

                    if (company == null)
                    {
                        return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
                    }

                    if (company.IsApproved)
                    {
                        return ApiResponseFactory.Error("Company is already approved.",
                            StatusCodes.Status400BadRequest);
                    }

                    // Approve company
                    company.IsApproved = true;
                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return ApiResponseFactory.Success("Company approved successfully.", StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return ApiResponseFactory.Error($"Error approving company: {ex.Message}",
                        StatusCodes.Status500InternalServerError);
                }
            });

        app.MapGet("/companies/pending",
            [Authorize(Roles = "globalAdmin")] async (
                ApplicationDbContext db,
                [FromQuery] string? search = null
            ) =>
            {
                try
                {
                    var pendingQuery = db.Companies.IgnoreQueryFilters()
                        .Where(c => !c.IsApproved && !c.IsDeleted);

                    // Optional name search
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        // Case-insensitive contains; use ILIKE for PostgreSQL
                        pendingQuery = pendingQuery.Where(c =>
                            EF.Functions.ILike(c.Name, $"%{search.Trim()}%"));
                    }

                    var pendingCompanies = await pendingQuery
                        .Include(c => c.Drivers)
                            .ThenInclude(d => d.User)
                        .Select(c => new CompanyDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Address = c.Address,
                            Postcode = c.Postcode,
                            City = c.City,
                            Country = c.Country,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Remark = c.Remark,
                            IsApproved = c.IsApproved,
                            Drivers = c.Drivers.Select(d => new DriverDto
                            {
                                DriverId = d.Id,
                                FirstName = d.User.FirstName,
                                LastName = d.User.LastName
                            }).ToList()
                        })
                        .ToListAsync();

                    return ApiResponseFactory.Success(pendingCompanies, StatusCodes.Status200OK);
                }
                catch (Exception ex)
                {
                    return ApiResponseFactory.Error($"Error fetching pending companies: {ex.Message}",
                        StatusCodes.Status500InternalServerError);
                }
            });

        return app;
    }
}