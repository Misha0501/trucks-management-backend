using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.Identity;
using TruckManagement.DTOs;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints
{
    public static class RateEndpoints
    {
        public static void MapRateEndpoints(this WebApplication app)
        {
            // Rate creation endpoint
            app.MapPost("/rates",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
                async (
                    [FromBody] CreateRateRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Validate basic fields
                        if (request == null || string.IsNullOrWhiteSpace(request.Name) ||
                            request.Value <= 0 ||
                            string.IsNullOrWhiteSpace(request.ClientId))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid payload. Name, Value, ClientId, and CompanyId are required.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // Validate GUID format
                        if (!Guid.TryParse(request.ClientId, out var clientGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid ClientId format.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // Check user identity
                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error(
                                "User not found or not authenticated.",
                                StatusCodes.Status401Unauthorized
                            );
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Fetch and validate client
                        var client = await db.Clients
                            .FirstOrDefaultAsync(c => c.Id == clientGuid);

                        if (client == null)
                        {
                            return ApiResponseFactory.Error(
                                "Specified client does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // If not globalAdmin, verify user's association via contact person
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

                            // Get all companies the contact person is linked to
                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            // Check if any of these companies actually own the specified client
                            bool clientBelongsToAssociatedCompany = await db.Clients
                                .AnyAsync(c => c.Id == clientGuid && associatedCompanyIds.Contains(c.CompanyId));

                            // Final validation
                            if (!clientBelongsToAssociatedCompany)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to create a rate for this company or client.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        // Create the rate
                        var rate = new Rate
                        {
                            Id = Guid.NewGuid(),
                            Name = request.Name,
                            Value = request.Value,
                            ClientId = clientGuid,
                            CompanyId = client.CompanyId,
                        };

                        db.Rates.Add(rate);
                        await db.SaveChangesAsync();

                        // Prepare response
                        var responseData = new
                        {
                            rate.Id,
                            rate.Name,
                            rate.Value,
                            rate.ClientId,
                            rate.CompanyId
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status201Created);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error creating rate: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "Internal server error while creating the rate.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                }
            );

            app.MapGet("/rates/{clientId}",
                [Authorize(Roles = "globalAdmin, customerAdmin, employer, customer, customerAccountant")]
                async (
                    string clientId,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser,
                    [FromQuery] int pageNumber = 1,
                    [FromQuery] int pageSize = 1000
                ) =>
                {
                    try
                    {
                        // Validate GUID format
                        if (!Guid.TryParse(clientId, out var clientGuid))
                        {
                            return ApiResponseFactory.Error("Invalid client ID format.",
                                StatusCodes.Status400BadRequest);
                        }

                        // Validate pagination parameters
                        if (pageNumber < 1 || pageSize < 1)
                        {
                            return ApiResponseFactory.Error("Invalid pagination parameters.",
                                StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");
                        bool isContactPerson =
                            !isGlobalAdmin; // Applies to customerAdmin, employer, customer, customerAccountant

                        // Base query: NO IgnoreQueryFilters() for global admin, everyone gets the same filtered view
                        var rateQuery = db.Rates.Where(r => r.ClientId == clientGuid);

                        if (isContactPerson)
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

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            var associatedClientIds = contactPerson.ContactPersonClientCompanies
                                .Where(cpc => cpc.ClientId.HasValue)
                                .Select(cpc => cpc.ClientId.Value)
                                .Distinct()
                                .ToList();

                            var clientData = await db.Clients
                                .Where(c => c.Id == clientGuid)
                                .Select(c => new { c.Id, c.CompanyId })
                                .FirstOrDefaultAsync();

                            if (clientData == null)
                            {
                                return ApiResponseFactory.Error(
                                    "Client does not exist or is not accessible.",
                                    StatusCodes.Status404NotFound
                                );
                            }

                            bool userIsAssociatedWithClientCompany =
                                associatedCompanyIds.Contains(clientData.CompanyId);
                            bool userIsDirectlyAssociatedWithClient = associatedClientIds.Contains(clientData.Id);

                            if (!userIsAssociatedWithClientCompany && !userIsDirectlyAssociatedWithClient)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to view rates for this client.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        // Get total count of rates for pagination
                        var totalRates = await rateQuery.CountAsync();
                        var totalPages = (int)Math.Ceiling((double)totalRates / pageSize);

                        // Paginate the rates
                        var ratesForClient = await rateQuery
                            .Include(r => r.Client)
                            .ThenInclude(c => c.Company)
                            .OrderBy(r => r.Name) // Sort rates alphabetically
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .Select(r => new
                            {
                                r.Id,
                                r.Name,
                                r.Value,
                                r.ClientId,
                                ClientName = r.Client.Name,
                                r.CompanyId,
                                CompanyName = r.Company.Name
                            })
                            .ToListAsync();

                        // Construct response
                        var responseData = new
                        {
                            totalRates,
                            totalPages,
                            pageNumber,
                            pageSize,
                            rates = ratesForClient
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching rates for client {clientId}: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An error occurred while retrieving the client rates.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                }
            );
            app.MapPut("/rates/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    string id,
                    [FromBody] UpdateRateRequest request,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        if (!Guid.TryParse(id, out Guid rateGuid))
                        {
                            return ApiResponseFactory.Error("Invalid rate ID format.", StatusCodes.Status400BadRequest);
                        }

                        if (request == null)
                        {
                            return ApiResponseFactory.Error(
                                "Request body is missing.",
                                StatusCodes.Status400BadRequest
                            );
                        }

                        // Ensure at least one updatable field is set
                        if (string.IsNullOrWhiteSpace(request.Name) && !request.Value.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "Nothing to update (Name or Value must be provided).",
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

                        // Fetch the Rate with its client & company
                        var rate = await db.Rates
                            .Include(r => r.Client)
                            .ThenInclude(c => c.Company)
                            .FirstOrDefaultAsync(r => r.Id == rateGuid);

                        if (rate == null)
                        {
                            return ApiResponseFactory.Error(
                                "Rate not found.",
                                StatusCodes.Status404NotFound
                            );
                        }

                        // If not global admin, do ownership checks
                        if (!isGlobalAdmin)
                        {
                            var isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                            if (!isCustomerAdmin)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to edit this rate.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

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

                            // Gather all companies the user is associated with
                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            // Client's owning company
                            var clientCompanyId = rate.Client.CompanyId;

                            if (!associatedCompanyIds.Contains(clientCompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to edit a rate whose client is outside your company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        // Update fields if provided
                        if (!string.IsNullOrWhiteSpace(request.Name))
                        {
                            rate.Name = request.Name;
                        }

                        if (request.Value.HasValue)
                        {
                            if (request.Value.Value <= 0)
                            {
                                return ApiResponseFactory.Error(
                                    "Value must be greater than zero.",
                                    StatusCodes.Status400BadRequest
                                );
                            }

                            rate.Value = request.Value.Value;
                        }

                        await db.SaveChangesAsync();

                        var responseData = new
                        {
                            rate.Id,
                            rate.Name,
                            rate.Value,
                            ClientId = rate.ClientId,
                            ClientName = rate.Client.Name,
                            CompanyId = rate.CompanyId,
                            CompanyName = rate.Client.Company.Name
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error updating rate: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while updating the rate.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
            
            app.MapDelete("/rates/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    string id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Validate ID format
                        if (!Guid.TryParse(id, out Guid rateGuid))
                        {
                            return ApiResponseFactory.Error("Invalid rate ID format.", StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.",
                                StatusCodes.Status401Unauthorized);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Fetch the rate with client + company
                        var rate = await db.Rates
                            .Include(r => r.Client)
                            .ThenInclude(c => c.Company)
                            .FirstOrDefaultAsync(r => r.Id == rateGuid);

                        if (rate == null)
                        {
                            return ApiResponseFactory.Error("Rate not found.", StatusCodes.Status404NotFound);
                        }

                        // If user is not global admin, check if user is a customer admin
                        if (!isGlobalAdmin)
                        {
                            bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                            if (!isCustomerAdmin)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to delete this rate.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

                            // Check if contact person is associated with the client's company
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

                            // Gather user-associated company IDs
                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            var clientCompanyId = rate.Client.CompanyId;
                            if (!associatedCompanyIds.Contains(clientCompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to delete a rate owned by a client outside your company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        // Delete the rate
                        db.Rates.Remove(rate);
                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success("Rate deleted successfully.", StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error deleting rate: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while deleting the rate.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
            
            app.MapGet("/rates/detail/{id}",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    string id,
                    ApplicationDbContext db,
                    UserManager<ApplicationUser> userManager,
                    ClaimsPrincipal currentUser
                ) =>
                {
                    try
                    {
                        // Validate GUID format
                        if (!Guid.TryParse(id, out var rateGuid))
                        {
                            return ApiResponseFactory.Error("Invalid rate ID format.", StatusCodes.Status400BadRequest);
                        }

                        var userId = userManager.GetUserId(currentUser);
                        if (string.IsNullOrEmpty(userId))
                        {
                            return ApiResponseFactory.Error("User not authenticated.", StatusCodes.Status401Unauthorized);
                        }

                        bool isGlobalAdmin = currentUser.IsInRole("globalAdmin");

                        // Fetch the Rate with its client & company
                        var rate = await db.Rates
                            .Include(r => r.Client)
                            .ThenInclude(c => c.Company)
                            .FirstOrDefaultAsync(r => r.Id == rateGuid);

                        if (rate == null)
                        {
                            return ApiResponseFactory.Error("Rate not found.", StatusCodes.Status404NotFound);
                        }

                        // If not global admin, ensure user is a customerAdmin & owns the client’s company
                        if (!isGlobalAdmin)
                        {
                            bool isCustomerAdmin = currentUser.IsInRole("customerAdmin");
                            if (!isCustomerAdmin)
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to access this rate detail.",
                                    StatusCodes.Status403Forbidden
                                );
                            }

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

                            var associatedCompanyIds = contactPerson.ContactPersonClientCompanies
                                .Select(cpc => cpc.CompanyId)
                                .Distinct()
                                .ToList();

                            var clientCompanyId = rate.Client.CompanyId;
                            if (!associatedCompanyIds.Contains(clientCompanyId))
                            {
                                return ApiResponseFactory.Error(
                                    "You are not authorized to view details of a rate owned by a client outside your company.",
                                    StatusCodes.Status403Forbidden
                                );
                            }
                        }

                        // Build the rate detail response
                        var responseData = new
                        {
                            rate.Id,
                            rate.Name,
                            rate.Value,
                            ClientId = rate.ClientId,
                            ClientName = rate.Client.Name,
                            CompanyId = rate.CompanyId,
                            CompanyName = rate.Client.Company.Name
                        };

                        return ApiResponseFactory.Success(responseData, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error fetching rate detail: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An unexpected error occurred while retrieving the rate detail.",
                            StatusCodes.Status500InternalServerError
                        );
                    }
                });
        }
    }
}