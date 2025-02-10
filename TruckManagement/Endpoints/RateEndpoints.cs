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
                            string.IsNullOrWhiteSpace(request.ClientId) ||
                            string.IsNullOrWhiteSpace(request.CompanyId))
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

                        if (!Guid.TryParse(request.CompanyId, out var companyGuid))
                        {
                            return ApiResponseFactory.Error(
                                "Invalid CompanyId format.",
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

                        // Fetch and validate company
                        var company = await db.Companies
                            .FirstOrDefaultAsync(c => c.Id == companyGuid);

                        if (company == null)
                        {
                            return ApiResponseFactory.Error(
                                "Specified company does not exist.",
                                StatusCodes.Status400BadRequest
                            );
                        }

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
                            if (!associatedCompanyIds.Contains(companyGuid) || !clientBelongsToAssociatedCompany)
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
                            CompanyId = companyGuid
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
        }
    }
}