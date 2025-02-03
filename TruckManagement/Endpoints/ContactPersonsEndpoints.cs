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
                    [FromQuery] Guid? companyId,
                    [FromQuery] Guid? clientId,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
                ) =>
                {
                    try
                    {
                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Build the base query
                        IQueryable<ContactPerson> query = db.ContactPersons
                            .Include(cp => cp.User)
                            .Include(cp => cp.ContactPersonClientCompanies).ThenInclude(cpc => cpc.Company)
                            .Include(cp => cp.ContactPersonClientCompanies).ThenInclude(cpc => cpc.Client)
                            .AsNoTracking();

                        // If not globalAdmin, the user must be a contact person with limited access
                        if (!isGlobalAdmin)
                        {
                            var myContact = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .ThenInclude(cpc => cpc.Client)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (myContact == null)
                            {
                                return ApiResponseFactory.Error(
                                    "ContactPerson profile not found.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Gather direct company IDs from ContactPersonClientCompanies
                            var directCompanyIds = myContact.ContactPersonClientCompanies
                                .Where(cpc => cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .Distinct()
                                .ToList();

                            // Gather client IDs and then find parent companies of those clients
                            var clientIds = myContact.ContactPersonClientCompanies
                                .Where(cpc => cpc.ClientId.HasValue)
                                .Select(cpc => cpc.ClientId.Value)
                                .Distinct()
                                .ToList();

                            var parentCompanyIds = await db.Clients
                                .Where(cl => clientIds.Contains(cl.Id))
                                .Select(cl => cl.CompanyId)
                                .Distinct()
                                .ToListAsync();

                            // Combine direct companies and the parent companies of any clients
                            var myCompanyIds = directCompanyIds
                                .Concat(parentCompanyIds)
                                .Distinct()
                                .ToList();

                            // If companyId is provided, ensure user is associated with that company
                            if (companyId.HasValue)
                            {
                                if (!myCompanyIds.Contains(companyId.Value))
                                {
                                    return ApiResponseFactory.Error(
                                        "Unauthorized: You are not associated with the specified company.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }

                                // Restrict query to contact persons associated with that company
                                query = query.Where(cp => cp.ContactPersonClientCompanies
                                    .Any(cpc => cpc.CompanyId == companyId.Value));
                            }

                            // If clientId is provided, ensure user is associated with the client's parent company
                            if (clientId.HasValue)
                            {
                                var client = await db.Clients
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == clientId.Value);

                                if (client == null)
                                {
                                    return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);
                                }

                                // If the user is not associated with the client's owning company, deny
                                if (!myCompanyIds.Contains(client.CompanyId))
                                {
                                    return ApiResponseFactory.Error(
                                        "Unauthorized: You are not associated with the client's company.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }

                                // Restrict query to contact persons who are associated with this client
                                // or with the company that owns this client
                                query = query.Where(cp => cp.ContactPersonClientCompanies.Any(cpc => cpc.ClientId == clientId.Value)
                                    || cp.ContactPersonClientCompanies.Any(cpc => cpc.CompanyId == client.CompanyId));
                            }

                            // If neither companyId nor clientId is provided, just restrict to the user's companies & their clients
                            if (!companyId.HasValue && !clientId.HasValue)
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
                            // If globalAdmin, optionally filter by companyId or clientId
                            if (companyId.HasValue)
                            {
                                query = query.Where(cp => cp.ContactPersonClientCompanies
                                    .Any(cpc => cpc.CompanyId == companyId.Value));
                            }
                            if (clientId.HasValue)
                            {
                                var client = await db.Clients
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == clientId.Value);

                                if (client == null)
                                {
                                    return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);
                                }

                                query = query.Where(cp => cp.ContactPersonClientCompanies.Any(cpc => cpc.ClientId == clientId.Value)
                                    || cp.ContactPersonClientCompanies.Any(cpc => cpc.CompanyId == client.CompanyId));
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
