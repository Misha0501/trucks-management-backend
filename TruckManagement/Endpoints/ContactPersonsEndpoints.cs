using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class ContactPersonsEndpoints
    {
        public static void MapContactPersonsEndpoints(this WebApplication app)
        {
            app.MapGet("/contactpersons",
                [Authorize(Roles = "globalAdmin, customerAdmin, customerAccountant, employer, customer")]
                async (
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] string? companyId,
                    [FromQuery] string? clientId,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
                ) =>
                {
                    try
                    {
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Attempt to parse the GUIDs, returning 400 if invalid format
                        Guid? parsedCompanyId = null;
                        if (!string.IsNullOrWhiteSpace(companyId))
                        {
                            if (!Guid.TryParse(companyId, out Guid validCompanyId))
                            {
                                return ApiResponseFactory.Error("Invalid companyId format. Must be a valid GUID.",
                                    StatusCodes.Status400BadRequest);
                            }

                            parsedCompanyId = validCompanyId;
                        }

                        Guid? parsedClientId = null;
                        if (!string.IsNullOrWhiteSpace(clientId))
                        {
                            if (!Guid.TryParse(clientId, out Guid validClientId))
                            {
                                return ApiResponseFactory.Error("Invalid clientId format. Must be a valid GUID.",
                                    StatusCodes.Status400BadRequest);
                            }

                            parsedClientId = validClientId;
                        }

                        IQueryable<ContactPerson> query = db.ContactPersons
                            .Include(cp => cp.User)
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .ThenInclude(cpc => cpc.Company)
                            .Include(cp => cp.ContactPersonClientCompanies)
                            .ThenInclude(cpc => cpc.Client)
                            .AsNoTracking();

                        if (!isGlobalAdmin)
                        {
                            var myContact = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .ThenInclude(cpc => cpc.Client)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (myContact == null)
                                return ApiResponseFactory.Error("ContactPerson profile not found.",
                                    StatusCodes.Status403Forbidden);

                            var directCompanyIds = myContact.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .Distinct()
                                .ToList();

                            var directClientIds = myContact.ContactPersonClientCompanies
                                .Where(cpc => cpc.ClientId.HasValue)
                                .Select(cpc => cpc.ClientId.Value)
                                .Distinct()
                                .ToList();

                            var parentCompanyIds = await db.Clients
                                .Where(cl => directClientIds.Contains(cl.Id))
                                .Select(cl => cl.CompanyId)
                                .Distinct()
                                .ToListAsync();

                            var myCompanyIds = directCompanyIds
                                .Concat(parentCompanyIds)
                                .Distinct()
                                .ToList();

                            // If companyId is provided, check existence & association
                            if (parsedCompanyId.HasValue)
                            {
                                var requestedCompany = await db.Companies
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == parsedCompanyId.Value);
                                if (requestedCompany == null)
                                    return ApiResponseFactory.Error("The specified company does not exist.",
                                        StatusCodes.Status404NotFound);

                                if (!myCompanyIds.Contains(parsedCompanyId.Value))
                                {
                                    return ApiResponseFactory.Error(
                                        "Unauthorized: You are not associated with the specified company.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }

                                query = query.Where(cp => cp.ContactPersonClientCompanies
                                    .Any(cpc => cpc.CompanyId == parsedCompanyId.Value));
                            }

                            // If clientId is provided, check existence & association
                            if (parsedClientId.HasValue)
                            {
                                var requestedClient = await db.Clients
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == parsedClientId.Value);
                                if (requestedClient == null)
                                    return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);

                                if (!myCompanyIds.Contains(requestedClient.CompanyId))
                                {
                                    return ApiResponseFactory.Error(
                                        "Unauthorized: You are not associated with the client's company.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }

                                query = query.Where(cp =>
                                    cp.ContactPersonClientCompanies.Any(cpc => cpc.ClientId == requestedClient.Id)
                                );
                            }

                            if (!parsedCompanyId.HasValue && !parsedClientId.HasValue)
                            {
                                var myClientIds = myContact.ContactPersonClientCompanies
                                    .Where(cpc => cpc.ClientId.HasValue)
                                    .Select(cpc => cpc.ClientId.Value)
                                    .Distinct()
                                    .ToList();

                                query = query.Where(cp => cp.ContactPersonClientCompanies.Any(cpc =>
                                    (cpc.CompanyId.HasValue && myCompanyIds.Contains(cpc.CompanyId.Value)) ||
                                    (cpc.ClientId.HasValue && myClientIds.Contains(cpc.ClientId.Value))
                                ));
                            }
                        }
                        else
                        {
                            // global admin flow
                            if (parsedCompanyId.HasValue)
                            {
                                var requestedCompany = await db.Companies
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == parsedCompanyId.Value);
                                if (requestedCompany == null)
                                    return ApiResponseFactory.Error("The specified company does not exist.",
                                        StatusCodes.Status404NotFound);

                                query = query.Where(cp =>
                                    cp.ContactPersonClientCompanies.Any(cpc => cpc.CompanyId == parsedCompanyId.Value));
                            }

                            if (parsedClientId.HasValue)
                            {
                                var requestedClient = await db.Clients
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == parsedClientId.Value);
                                if (requestedClient == null)
                                    return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);

                                query = query.Where(cp =>
                                    cp.ContactPersonClientCompanies.Any(cpc => cpc.ClientId == requestedClient.Id)
                                );
                            }
                        }

                        var totalCount = await query.CountAsync();
                        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                        var contactPersons = await query
                            .OrderBy(cp => cp.User.Email)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(cp => new
                            {
                                ContactPersonId = cp.Id,
                                User = new
                                {
                                    cp.User.Id,
                                    cp.User.Email,
                                    cp.User.FirstName,
                                    cp.User.LastName
                                },
                                AssociatedCompanies = cp.ContactPersonClientCompanies
                                    .Where(cpc => cpc.Company != null)
                                    .Select(cpc => new
                                    {
                                        cpc.Company.Id,
                                        cpc.Company.Name
                                    })
                                    .Distinct()
                                    .ToList(),
                                AssociatedClients = cp.ContactPersonClientCompanies
                                    .Where(cpc => cpc.Client != null)
                                    .Select(cpc => new
                                    {
                                        cpc.Client.Id,
                                        cpc.Client.Name
                                    })
                                    .Distinct()
                                    .ToList()
                            })
                            .ToListAsync();

                        var responseData = new
                        {
                            TotalCount = totalCount,
                            TotalPages = totalPages,
                            PageNumber = pageNumber,
                            PageSize = pageSize,
                            Data = contactPersons
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while fetching contact persons.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}