using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.DTOs;
using TruckManagement.Entities;
using TruckManagement.Helpers;
using TruckManagement.Models; // Where ApiResponseFactory is defined

namespace TruckManagement.Endpoints
{
    public static class CharterEndpoints
    {
        public static void MapCharterEndpoints(this WebApplication app)
        {
            app.MapPost("/charters",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    [FromBody] CreateCharterRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (request == null
                            || string.IsNullOrWhiteSpace(request.Name)
                            || string.IsNullOrWhiteSpace(request.CompanyId)
                            || string.IsNullOrWhiteSpace(request.ClientId))
                        {
                            return ApiResponseFactory.Error(
                                "Name, CompanyId, and ClientId are required.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        if (!Guid.TryParse(request.CompanyId, out var companyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid CompanyId format.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        if (!Guid.TryParse(request.ClientId, out var clientGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid ClientId format.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Check if the provided company exists
                        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyGuid);
                        if (company == null)
                        {
                            return ApiResponseFactory.Error(
                                "The specified company does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // Check if the provided client exists and belongs to the same company
                        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == clientGuid);
                        if (client == null)
                        {
                            return ApiResponseFactory.Error(
                                "The specified client does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        if (client.CompanyId != companyGuid)
                        {
                            return ApiResponseFactory.Error(
                                "This client does not belong to the provided company.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // If not global admin, confirm user is associated with the provided company
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Gather all associated company IDs for the contact person
                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            // Ensure user is associated with the same company
                            if (!associatedCompanyIds.Contains(companyGuid))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create a charter in this company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        var charter = new Charter
                        {
                            Id = Guid.NewGuid(),
                            Name = request.Name,
                            CompanyId = companyGuid,
                            ClientId = clientGuid,
                            Remark = request.Remark
                        };

                        db.Charters.Add(charter);
                        await db.SaveChangesAsync();

                        var responseData = new
                        {
                            charter.Id,
                            charter.Name,
                            charter.CompanyId,
                            charter.ClientId,
                            charter.Remark
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error creating charter: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while creating the charter.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapGet("/charters",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
                async (
                    [FromQuery] string? companyId,
                    [FromQuery] string? clientId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 10
                ) =>
                {
                    try
                    {
                        // Validate pagination
                        if (pageNumber < 1 || pageSize < 1)
                        {
                            return ApiResponseFactory.Error(
                                "Invalid pagination parameters (pageNumber, pageSize).",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Base query for Charters
                        var query = db.Charters.AsQueryable();

                        // If not global admin, gather the user's associated company IDs and client IDs
                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // The user might be bridged to companies or directly to some clients
                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            var associatedClientIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.ClientId.HasValue)
                                .Select(cpc => cpc.ClientId.Value)
                                .Distinct()
                                .ToList();

                            // Filter charters by:
                            // - Company is in associatedCompanyIds
                            // OR
                            // - Client is in associatedClientIds
                            query = query.Where(ch =>
                                associatedCompanyIds.Contains(ch.CompanyId)
                                || associatedClientIds.Contains(ch.ClientId));
                        }

                        // If ?companyId= is provided, filter further
                        if (!string.IsNullOrEmpty(companyId))
                        {
                            if (!Guid.TryParse(companyId, out var companyGuid))
                            {
                                return ApiResponseFactory.Error(
                                    "Invalid companyId in query string.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            query = query.Where(ch => ch.CompanyId == companyGuid);
                        }

                        // If ?clientId= is provided, filter further
                        if (!string.IsNullOrEmpty(clientId))
                        {
                            if (!Guid.TryParse(clientId, out var clientGuid))
                            {
                                return ApiResponseFactory.Error(
                                    "Invalid clientId in query string.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            query = query.Where(ch => ch.ClientId == clientGuid);
                        }

                        // Count total charters
                        var totalCharters = await query.CountAsync();
                        var totalPages = (int)Math.Ceiling((double)totalCharters / pageSize);

                        // If offset out of range, return empty array
                        var offset = (pageNumber - 1) * pageSize;
                        if (offset >= totalCharters)
                        {
                            return ApiResponseFactory.Success(new
                            {
                                totalCharters,
                                totalPages,
                                pageNumber,
                                pageSize,
                                data = new List<object>() // empty
                            }, StatusCodes.Status200OK);
                        }

                        // Perform the pagination
                        var charters = await query
                            .OrderBy(ch => ch.Name)
                            .Skip(offset)
                            .Take(pageSize)
                            .Select(ch => new
                            {
                                ch.Id,
                                ch.Name,
                                ch.Remark,
                                ch.ClientId,
                                ch.CompanyId
                            })
                            .ToListAsync();

                        var responseData = new
                        {
                            totalCharters,
                            totalPages,
                            pageNumber,
                            pageSize,
                            data = charters
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching charters: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while listing charters.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });

            app.MapPut("/charters/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    string id,
                    [FromBody] UpdateCharterRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (!Guid.TryParse(id, out var charterGuid))
                        {
                            return ApiResponseFactory.Error("Invalid charter ID format.",
                                StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                        if (!isGlobalAdmin && !isCustomerAdmin)
                        {
                            return ApiResponseFactory.Error("You are not authorized to edit charters.",
                                StatusCodes.Status403Forbidden);
                        }

                        var charter = await db.Charters
                            .Include(ch => ch.Client)
                            .ThenInclude(cl => cl.Company)
                            .FirstOrDefaultAsync(ch => ch.Id == charterGuid);

                        if (charter == null)
                        {
                            return ApiResponseFactory.Error("Charter not found.", StatusCodes.Status404NotFound);
                        }

                        if (!isGlobalAdmin)
                        {
                            var contactPerson = await db.ContactPersons
                                .Include(cp => cp.ContactPersonClientCompanies)
                                .FirstOrDefaultAsync(cp => cp.AspNetUserId == userId);

                            if (contactPerson == null)
                            {
                                return ApiResponseFactory.Error(
                                    "No contact person profile found. You are not authorized.",
                                    StatusCodes.Status403Forbidden);
                            }

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            // If user doesn't provide a new ClientId, check old one
                            if (string.IsNullOrWhiteSpace(request.ClientId))
                            {
                                if (!associatedCompanyIds.Contains(charter.Client.CompanyId))
                                {
                                    return ApiResponseFactory.Error(
                                        "You are not authorized to edit a charter outside your company's clients.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }
                            }
                            else
                            {
                                // If the user wants to assign a new client
                                if (!Guid.TryParse(request.ClientId, out var newClientGuid))
                                {
                                    return ApiResponseFactory.Error("Invalid new ClientId format.",
                                        StatusCodes.Status400BadRequest);
                                }

                                var newClient = await db.Clients
                                    .Include(cl => cl.Company)
                                    .FirstOrDefaultAsync(cl => cl.Id == newClientGuid);

                                if (newClient == null)
                                {
                                    return ApiResponseFactory.Error("The specified new client does not exist.",
                                        StatusCodes.Status400BadRequest);
                                }

                                // Ensure user can access the new client's company
                                if (!associatedCompanyIds.Contains(newClient.CompanyId))
                                {
                                    return ApiResponseFactory.Error(
                                        "You are not authorized to assign the charter to a client from a company you do not belong to.",
                                        StatusCodes.Status403Forbidden
                                    );
                                }

                                // Update the charter's client & company
                                charter.ClientId = newClientGuid;
                                charter.CompanyId = newClient.CompanyId;
                            }
                        }
                        else
                        {
                            // If user is global admin and provided a new client, just do the normal check
                            if (!string.IsNullOrWhiteSpace(request.ClientId))
                            {
                                if (!Guid.TryParse(request.ClientId, out var newClientGuid))
                                {
                                    return ApiResponseFactory.Error("Invalid new ClientId format.",
                                        StatusCodes.Status400BadRequest);
                                }

                                var newClient = await db.Clients
                                    .Include(cl => cl.Company)
                                    .FirstOrDefaultAsync(cl => cl.Id == newClientGuid);

                                if (newClient == null)
                                {
                                    return ApiResponseFactory.Error("The specified new client does not exist.",
                                        StatusCodes.Status400BadRequest);
                                }

                                charter.ClientId = newClientGuid;
                                charter.CompanyId = newClient.CompanyId;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(request.Name))
                        {
                            charter.Name = request.Name;
                        }

                        if (!string.IsNullOrWhiteSpace(request.Remark))
                        {
                            charter.Remark = request.Remark;
                        }

                        await db.SaveChangesAsync();

                        var responseData = new
                        {
                            charter.Id,
                            charter.Name,
                            charter.ClientId,
                            charter.CompanyId,
                            charter.Remark
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error updating charter: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the charter.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}