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
        }
    }
}