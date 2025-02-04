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

                        if (request.CompanyId == Guid.Empty || request.ClientId == Guid.Empty)
                            return ApiResponseFactory.Error("CompanyId and ClientId are required.",
                                StatusCodes.Status400BadRequest);

                        var currentUserId = userManager.GetUserId(currentUser);
                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");

                        if (!isGlobalAdmin && !isCustomerAdmin)
                        {
                            return ApiResponseFactory.Error("Unauthorized to create a surcharge.",
                                StatusCodes.Status403Forbidden);
                        }

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

                            var directCompanyIds = await db.ContactPersonClientCompanies
                                .Where(cpc => cpc.ContactPersonId == contactPerson.Id && cpc.CompanyId.HasValue)
                                .Select(cpc => cpc.CompanyId.Value)
                                .Distinct()
                                .ToListAsync();

                            var clientsOwnedByCompanies = await db.Clients
                                .Where(client => directCompanyIds.Contains(client.CompanyId))
                                .Select(client => client.Id)
                                .Distinct()
                                .ToListAsync();

                            if (!directCompanyIds.Contains(request.CompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create surcharges for this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            var requestedClient = await db.Clients
                                .AsNoTracking()
                                .FirstOrDefaultAsync(c => c.Id == request.ClientId);

                            if (requestedClient == null)
                            {
                                return ApiResponseFactory.Error("Client not found.", StatusCodes.Status404NotFound);
                            }

                            if (!directCompanyIds.Contains(requestedClient.CompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create surcharges for this client.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            if (!clientsOwnedByCompanies.Contains(request.ClientId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create surcharges for this client.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        var surcharge = new Surcharge
                        {
                            Id = Guid.NewGuid(),
                            Value = request.Value,
                            ClientId = request.ClientId,
                            CompanyId = request.CompanyId
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
                    ClaimsPrincipal currentUser
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

                        var surcharges = await db.Surcharges
                            .Where(s => s.ClientId == validClientId)
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

                        return ApiResponseFactory.Success(surcharges, StatusCodes.Status200OK);
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