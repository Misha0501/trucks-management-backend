using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers; // Assuming ApiResponseFactory is in this namespace

namespace TruckManagement.Routes
{
    public static class ClientsRoute
    {
        public static void RegisterClientsRoutes(this IEndpointRouteBuilder app)
        {
            app.MapGet("/clients",
                [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer, driver")]
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
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
                    bool isDriver = currentUser.IsInRole("driver");

                    bool isContactPerson = isCustomerAdmin || isCustomerAccountant || isEmployer || isCustomer;

                    // 3. If the user is not a globalAdmin, ContactPerson, or Driver, deny access
                    if (!isGlobalAdmin && !isContactPerson && !isDriver)
                    {
                        return ApiResponseFactory.Error("Unauthorized to view clients.",
                            StatusCodes.Status403Forbidden);
                    }

                    // 4. Initialize lists to hold CompanyIds and ClientIds for filtering
                    List<Guid> companyIds = new List<Guid>();
                    List<Guid> clientIds = new List<Guid>();

                    if (isContactPerson)
                    {
                        // Retrieve the ContactPerson entity associated with the current user
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                        if (contactPerson != null)
                        {
                            // Retrieve associated CompanyIds from ContactPersonClientCompany
                            companyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .Distinct()
                                .ToList();

                            // Retrieve associated ClientIds from ContactPersonClientCompany
                            clientIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.ClientId.HasValue)
                                .Select(cpc => cpc.ClientId.Value)
                                .Distinct()
                                .ToList();
                        }
                        else
                        {
                            return ApiResponseFactory.Error("ContactPerson profile not found.",
                                StatusCodes.Status403Forbidden);
                        }
                    }

                    if (isDriver)
                    {
                        // Retrieve the Driver entity associated with the current user
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == currentUserId);

                        if (driver != null && driver.CompanyId.HasValue)
                        {
                            companyIds.Add(driver.CompanyId.Value);
                        }
                        else
                        {
                            return ApiResponseFactory.Error(
                                "Driver profile not found or not associated with any company.",
                                StatusCodes.Status403Forbidden);
                        }
                    }

                    // 5. Build the base query based on the user's role
                    IQueryable<Client> clientsQuery = db.Clients.AsQueryable();

                    if (isContactPerson || isDriver)
                    {
                        // Restrict to clients associated with the ContactPerson's companies OR directly assigned clients
                        clientsQuery = clientsQuery.Where(c =>
                            companyIds.Contains(c.CompanyId) || clientIds.Contains(c.Id));
                    }
                    // If globalAdmin, no additional filtering is needed

                    // 6. Get total client count after filtering for pagination
                    var totalClients = await clientsQuery.CountAsync();
                    var totalPages = (int)Math.Ceiling((double)totalClients / pageSize);

                    // 7. Apply pagination and select necessary fields
                    var pagedClients = await clientsQuery
                        .AsNoTracking()
                        .Include(c => c.Company)
                        .OrderBy(c => c.Name)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(c => new ClientDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Tav = c.Tav,
                            Address = c.Address,
                            Postcode = c.Postcode,
                            City = c.City,
                            Country = c.Country,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Remark = c.Remark,
                            Company = new CompanyDto
                            {
                                Id = c.Company.Id,
                                Name = c.Company.Name
                            }
                        })
                        .ToListAsync();

                    // 8. Build the response object with pagination info
                    var responseData = new
                    {
                        totalClients,
                        totalPages,
                        pageNumber,
                        pageSize,
                        data = pagedClients
                    };

                    // 9. Return success response
                    return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                });

            app.MapGet("/clients/{id:guid}",
                [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer, driver")]
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
                    bool isDriver = currentUser.IsInRole("driver");

                    bool isContactPerson = isCustomerAdmin || isCustomerAccountant || isEmployer || isCustomer;

                    // 3. If the user is not a globalAdmin, ContactPerson, or Driver, deny access
                    if (!isGlobalAdmin && !isContactPerson && !isDriver)
                    {
                        return ApiResponseFactory.Error("Unauthorized to view client details.",
                            StatusCodes.Status403Forbidden);
                    }

                    // 4. Initialize lists to hold CompanyIds and ClientIds for filtering
                    List<Guid> companyIds = new List<Guid>();
                    List<Guid> clientIds = new List<Guid>();

                    if (isContactPerson)
                    {
                        // Retrieve the ContactPerson entity associated with the current user
                        var contactPerson = await db.ContactPersons
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                        if (contactPerson != null)
                        {
                            // Retrieve associated CompanyIds from ContactPersonClientCompany
                            companyIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .Distinct()
                                .ToList();

                            // Retrieve associated ClientIds from ContactPersonClientCompany
                            clientIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.ClientId.HasValue)
                                .Select(cpc => cpc.ClientId.Value)
                                .Distinct()
                                .ToList();
                        }
                        else
                        {
                            return ApiResponseFactory.Error("ContactPerson profile not found.",
                                StatusCodes.Status403Forbidden);
                        }
                    }

                    if (isDriver)
                    {
                        // Retrieve the Driver entity associated with the current user
                        var driver = await db.Drivers
                            .FirstOrDefaultAsync(d => d.AspNetUserId == currentUserId);

                        if (driver != null && driver.CompanyId.HasValue)
                        {
                            companyIds.Add(driver.CompanyId.Value);
                        }
                        else
                        {
                            return ApiResponseFactory.Error(
                                "Driver profile not found or not associated with any company.",
                                StatusCodes.Status403Forbidden);
                        }
                    }

                    // 5. Fetch the client by ID
                    var clientQuery = db.Clients
                        .AsNoTracking()
                        .Include(c => c.Company)
                        .Where(c => c.Id == id);

                    // 6. Apply access restrictions if not a global admin
                    if (!isGlobalAdmin)
                    {
                        clientQuery = clientQuery.Where(c =>
                            companyIds.Contains(c.CompanyId) || clientIds.Contains(c.Id));
                    }

                    var client = await clientQuery.Select(c => new ClientDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Tav = c.Tav,
                        Address = c.Address,
                        Postcode = c.Postcode,
                        City = c.City,
                        Country = c.Country,
                        PhoneNumber = c.PhoneNumber,
                        Email = c.Email,
                        Remark = c.Remark,
                        Company = new CompanyDto
                        {
                            Id = c.Company.Id,
                            Name = c.Company.Name
                        }
                    }).FirstOrDefaultAsync();

                    if (client == null)
                    {
                        return ApiResponseFactory.Error("Client not found or access denied.",
                            StatusCodes.Status404NotFound);
                    }

                    // 7. Return success response
                    return ApiResponseFactory.Success(client, StatusCodes.Status200OK);
                });

            app.MapPost("/clients",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    [FromBody] CreateClientRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // 1️⃣ Validate input
                        if (request == null || string.IsNullOrWhiteSpace(request.Name) ||
                            request.CompanyId == Guid.Empty)
                        {
                            return ApiResponseFactory.Error("Invalid request. Name and CompanyId are required.",
                                StatusCodes.Status400BadRequest);
                        }

                        // 2️⃣ Retrieve the current user's ID
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        // 3️⃣ Check if the company exists
                        var companyExists = await db.Companies.AnyAsync(c => c.Id == request.CompanyId);
                        if (!companyExists)
                        {
                            return ApiResponseFactory.Error("The specified company does not exist.",
                                StatusCodes.Status400BadRequest);
                        }

                        // 4️⃣ If the user is a Customer Admin, verify they are associated with the company
                        if (isCustomerAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("Unauthorized: No ContactPerson profile found.",
                                    StatusCodes.Status403Forbidden);
                            }

                            bool isAssociated = contactPerson.ContactPersonClientCompanies
                                .Any(cpc => cpc.CompanyId == request.CompanyId);

                            if (!isAssociated)
                            {
                                return ApiResponseFactory.Error(
                                    "Unauthorized: You cannot add clients to a company you are not associated with.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        // 5️⃣ Create new Client
                        var newClient = new Client
                        {
                            Id = Guid.NewGuid(),
                            Name = request.Name,
                            Tav = request.Tav,
                            Address = request.Address,
                            Postcode = request.Postcode,
                            City = request.City,
                            Country = request.Country,
                            PhoneNumber = request.PhoneNumber,
                            Email = request.Email,
                            Remark = request.Remark,
                            CompanyId = request.CompanyId
                        };

                        db.Clients.Add(newClient);
                        await db.SaveChangesAsync();

                        // 6️⃣ Return success response
                        return ApiResponseFactory.Success(new
                        {
                            newClient.Id,
                            newClient.Name,
                            newClient.Tav,
                            newClient.Address,
                            newClient.Postcode,
                            newClient.City,
                            newClient.Country,
                            newClient.PhoneNumber,
                            newClient.Email,
                            newClient.Remark,
                            newClient.CompanyId
                        }, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        // 7️⃣ Handle unexpected errors (e.g., EF exceptions, DB issues)
                        //    Log the exception as needed for diagnostics
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[Stack] {ex.StackTrace}");

                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while creating the client.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapDelete("/clients/{id:guid}",
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
                        // 1️⃣ Retrieve the current user's ID and roles
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        // 2️⃣ Find the client by ID
                        var client = await db.Clients.FindAsync(id);
                        if (client == null)
                        {
                            return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);
                        }

                        // 3️⃣ If the user is a Global Admin, skip further checks
                        if (isGlobalAdmin)
                        {
                            db.Clients.Remove(client);
                            await db.SaveChangesAsync();
                            return ApiResponseFactory.Success("Client removed successfully.", StatusCodes.Status200OK);
                        }

                        // 4️⃣ If the user is a Customer Admin, validate their association with the client's company
                        if (isCustomerAdmin)
                        {
                            // Retrieve associated ContactPerson
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("No ContactPerson profile found.",
                                    StatusCodes.Status403Forbidden);
                            }

                            // Check if the user is associated with the company's ID of the client
                            bool isAssociated = contactPerson.ContactPersonClientCompanies
                                .Any(cpc => cpc.CompanyId == client.CompanyId);

                            if (!isAssociated)
                            {
                                return ApiResponseFactory.Error(
                                    "Unauthorized: You cannot remove clients from a company you are not associated with.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // 5️⃣ If valid association, remove the client
                            db.Clients.Remove(client);
                            await db.SaveChangesAsync();
                            return ApiResponseFactory.Success("Client removed successfully.", StatusCodes.Status200OK);
                        }

                        // 6️⃣ If none of the roles matched, deny access
                        return ApiResponseFactory.Error("Unauthorized to remove the client.",
                            StatusCodes.Status403Forbidden);
                    }
                    catch (Exception ex)
                    {
                        // 7️⃣ Handle unexpected errors (EF exceptions, DB connection issues, etc.)
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[Stack] {ex.StackTrace}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while removing the client.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapPut("/clients/{id:guid}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid id,
                    [FromBody] UpdateClientRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Basic validation: Name is mandatory
                        if (request == null || string.IsNullOrWhiteSpace(request.Name))
                        {
                            return ApiResponseFactory.Error("Invalid request. Name is required.",
                                StatusCodes.Status400BadRequest);
                        }

                        // Retrieve user info
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        // If user is neither globalAdmin nor customerAdmin, deny access
                        if (!isGlobalAdmin && !isCustomerAdmin)
                        {
                            return ApiResponseFactory.Error("Unauthorized to edit the client.",
                                StatusCodes.Status403Forbidden);
                        }

                        // Find the target client
                        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id);
                        if (client == null)
                        {
                            return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);
                        }

                        // If user is global admin, no further checks needed
                        if (!isGlobalAdmin)
                        {
                            // For a customer admin, ensure they are a contact person
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("No ContactPerson profile found.",
                                    StatusCodes.Status403Forbidden);
                            }

                            // Verify user can still edit the existing company
                            bool isAssociatedWithCurrentCompany = contactPerson.ContactPersonClientCompanies
                                .Any(cpc => cpc.CompanyId == client.CompanyId);

                            if (!isAssociatedWithCurrentCompany)
                            {
                                return ApiResponseFactory.Error(
                                    "You cannot edit clients of a company you are not associated with.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // If customer admin wants to change company, ensure association with the new one
                            if (request.CompanyId.HasValue && request.CompanyId.Value != client.CompanyId)
                            {
                                bool isAssociatedWithNewCompany = contactPerson.ContactPersonClientCompanies
                                    .Any(cpc => cpc.CompanyId == request.CompanyId.Value);

                                if (!isAssociatedWithNewCompany)
                                {
                                    return ApiResponseFactory.Error(
                                        "You cannot assign this client to a company you are not associated with.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }
                            }
                        }

                        // Update fields
                        client.Name = request.Name;
                        client.Tav = request.Tav;
                        client.Address = request.Address;
                        client.Postcode = request.Postcode;
                        client.City = request.City;
                        client.Country = request.Country;
                        client.PhoneNumber = request.PhoneNumber;
                        client.Email = request.Email;
                        client.Remark = request.Remark;

                        // Update CompanyId if provided
                        if (request.CompanyId.HasValue && request.CompanyId.Value != client.CompanyId)
                        {
                            // Double-check the new company exists (for safety)
                            bool newCompanyExists = await db.Companies.AnyAsync(c => c.Id == request.CompanyId.Value);
                            if (!newCompanyExists)
                            {
                                return ApiResponseFactory.Error(
                                    "The specified new company does not exist.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            client.CompanyId = request.CompanyId.Value;
                        }

                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success(new
                        {
                            client.Id,
                            client.Name,
                            client.Tav,
                            client.Address,
                            client.Postcode,
                            client.City,
                            client.Country,
                            client.PhoneNumber,
                            client.Email,
                            client.Remark,
                            client.CompanyId
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log error details (for debugging)
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");

                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the client.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}