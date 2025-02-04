using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Api.Endpoints
{
    public static class SurchargeEndpoints
    {
        public static void MapSurchargeEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/surcharges",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    [FromBody] CreateSurchargeRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (request == null)
                            return ApiResponseFactory.Error("Invalid request body.", StatusCodes.Status400BadRequest);

                        if (request.Value <= 0)
                            return ApiResponseFactory.Error("Surcharge Value must be greater than 0.",
                                StatusCodes.Status400BadRequest);

                        if (!Guid.TryParse(request.CompanyId, out Guid validCompanyId))
                            return ApiResponseFactory.Error("Invalid CompanyId format. Must be a valid GUID.",
                                StatusCodes.Status400BadRequest);

                        if (!Guid.TryParse(request.ClientId, out Guid validClientId))
                            return ApiResponseFactory.Error("Invalid ClientId format. Must be a valid GUID.",
                                StatusCodes.Status400BadRequest);

                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        if (!isGlobalAdmin && !isCustomerAdmin)
                        {
                            return ApiResponseFactory.Error("Unauthorized to create a surcharge.",
                                StatusCodes.Status403Forbidden);
                        }

                        List<Guid> userCompanyIds = new();
                        List<Guid> clientsOwnedByCompanies = new();

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error("No ContactPerson profile found.",
                                    StatusCodes.Status403Forbidden);
                            }

                            userCompanyIds = await db.ContactPersonClientCompanies
                                .Where(cpc => cpc.ContactPersonId == contactPerson.Id && cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .Distinct()
                                .ToListAsync();

                            clientsOwnedByCompanies = await db.Clients
                                .Where(client => userCompanyIds.Contains(client.CompanyId))
                                .Select(client => client.Id)
                                .Distinct()
                                .ToListAsync();

                            if (!userCompanyIds.Contains(validCompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create surcharges for this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            if (!clientsOwnedByCompanies.Contains(validClientId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create surcharges for this client.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        var requestedCompany = await db.Companies.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == validCompanyId);
                        if (requestedCompany == null)
                        {
                            return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
                        }

                        var requestedClient =
                            await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == validClientId);
                        if (requestedClient == null)
                        {
                            return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);
                        }

                        if (requestedClient.CompanyId != validCompanyId)
                        {
                            return ApiResponseFactory.Error(
                                "The specified client does not belong to the given company.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        var surcharge = new Surcharge
                        {
                            Id = Guid.NewGuid(),
                            Value = request.Value,
                            ClientId = validClientId,
                            CompanyId = validCompanyId
                        };

                        db.Surcharges.Add(surcharge);
                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success(new
                        {
                            surcharge.Id,
                            surcharge.Value,
                            surcharge.ClientId,
                            surcharge.CompanyId
                        }, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");

                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while creating the surcharge.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });


            app.MapGet("/surcharges/{clientId}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    string clientId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
                ) =>
                {
                    try
                    {
                        if (!Guid.TryParse(clientId, out Guid validClientId))
                        {
                            return ApiResponseFactory.Error("Invalid clientId format. Must be a valid GUID.",
                                StatusCodes.Status400BadRequest);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        if (!isGlobalAdmin && !isCustomerAdmin)
                        {
                            return ApiResponseFactory.Error("Unauthorized to view surcharges.",
                                StatusCodes.Status403Forbidden);
                        }

                        List<Guid> userCompanyIds = new();
                        List<Guid> clientsOwnedByCompanies = new();

                        if (!isGlobalAdmin)
                        {
                            var currentUserId = userManager.GetUserId(currentUser);
                            var currentContactPerson = await db.ContactPersons
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == currentUserId);

                            if (currentContactPerson == null)
                            {
                                return ApiResponseFactory.Error("ContactPerson profile not found.",
                                    StatusCodes.Status403Forbidden);
                            }

                            userCompanyIds = await db.ContactPersonClientCompanies
                                .Where(cpc => cpc.ContactPersonId == currentContactPerson.Id && cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .ToListAsync();

                            clientsOwnedByCompanies = await db.Clients
                                .Where(cl => userCompanyIds.Contains(cl.CompanyId))
                                .Select(cl => cl.Id)
                                .ToListAsync();

                            if (!clientsOwnedByCompanies.Contains(validClientId))
                            {
                                return ApiResponseFactory.Error(
                                    "Unauthorized: The specified client is not associated with your company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        // Count total surcharges for pagination
                        var totalSurcharges = await db.Surcharges
                            .Where(s => s.ClientId == validClientId)
                            .CountAsync();

                        if (totalSurcharges == 0)
                        {
                            return ApiResponseFactory.Error("No surcharges found for this client.",
                                StatusCodes.Status404NotFound);
                        }

                        var totalPages = (int)Math.Ceiling((double)totalSurcharges / pageSize);

                        var paginatedSurcharges = await db.Surcharges
                            .Where(s => s.ClientId == validClientId)
                            .OrderBy(s => s.Id) // Ordering to ensure consistent pagination
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(s => new
                            {
                                s.Id,
                                s.Value,
                                Client = new
                                {
                                    s.Client.Id,
                                    s.Client.Name
                                },
                                Company = new
                                {
                                    s.Company.Id,
                                    s.Company.Name
                                }
                            })
                            .ToListAsync();

                        var response = new
                        {
                            TotalSurcharges = totalSurcharges,
                            TotalPages = totalPages,
                            PageNumber = pageNumber,
                            PageSize = pageSize,
                            Data = paginatedSurcharges
                        };

                        return ApiResponseFactory.Success(response, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] {ex.Message}");
                        Console.WriteLine($"[StackTrace] {ex.StackTrace}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while fetching surcharges.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}